// Copyright (C) 2021, Bastian Urbach
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static Utility;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class NonStarShapedRaytracedLight : CustomDeferredLight {
    public NonConvexSilhouetteBSP silhouetteBSP;
    public SilhouetteTriangulationBSP triangulationBSP;
	public LtcLookupTable ggxLookupTable;
	public RayTracingShader shader;

	public int raysPerPixel;
	public Material additiveBlit;

	[ColorUsage(false, true)]
	public Color color;
	
	ComputeBuffer vertices;
	ComputeBuffer nodes;
	ComputeBuffer silhouettes;
	ComputeBuffer triangulations;
	ComputeBuffer triangulationNodes;
	MaterialPropertyBlock materialProperties;

	struct State {
		public CommandBuffer commandBuffer;
	}

	public override void InitializeLight() {
		vertices = silhouetteBSP.GetVertexBuffer();
		nodes = silhouetteBSP.GetNodeBuffer();
		silhouettes = silhouetteBSP.GetSilhouetteBuffer();

		triangulations = triangulationBSP.GetSilhouetteBuffer();
		triangulationNodes = triangulationBSP.GetNodeBuffer();

		materialProperties = new MaterialPropertyBlock();
		materialProperties.SetInt("_LightSourceID", gameObject.GetInstanceID());

		var renderer = GetComponent<Renderer>();
		renderer.SetPropertyBlock(materialProperties);

		var meshFilter = GetComponent<MeshFilter>();
		meshFilter.sharedMesh = silhouetteBSP.mesh;
	}

	public override void UpdateLight() {
		Color.RGBToHSV(color, out var h, out var s, out var v);
		materialProperties.SetVector("_Color", Color.HSVToRGB(h, s, 1));
		var renderer = GetComponent<Renderer>();
		renderer.SetPropertyBlock(materialProperties);
	}

	public override void InitializeCamera(Camera camera, out object state) {
		State s = default;

		s.commandBuffer = new CommandBuffer();
		s.commandBuffer.name = gameObject.name;
		camera.AddCommandBuffer(CameraEvent.AfterLighting, s.commandBuffer);

		state = s;
	}

	public override void UpdateCamera(Camera camera, ref object state) {
		var s = (State)state;

		var target = Raytracing.GetTarget(camera);
		var accelerator = Raytracing.GetAccelerator();
		var lightSourceID = gameObject.GetInstanceID();

		s.commandBuffer.Clear();

		SetShaderProperty(s.commandBuffer, shader, "_Color", color);
		SetShaderProperty(s.commandBuffer, shader, "_LightToWorld", transform.localToWorldMatrix);
		SetShaderProperty(s.commandBuffer, shader, "_WorldToLight", transform.worldToLocalMatrix);
		SetShaderProperty(s.commandBuffer, shader, "_Vertices", vertices);
		SetShaderProperty(s.commandBuffer, shader, "_Nodes", nodes);
		SetShaderProperty(s.commandBuffer, shader, "_TriangulationNodes", triangulationNodes);
		SetShaderProperty(s.commandBuffer, shader, "_SilhouetteEdges", silhouettes);
		SetShaderProperty(s.commandBuffer, shader, "_SilhouetteTriangles", triangulations);
		SetShaderProperty(s.commandBuffer, shader, "_Root", silhouetteBSP.root);
		SetShaderProperty(s.commandBuffer, shader, "_RaysPerPixel", raysPerPixel);
		SetShaderProperty(s.commandBuffer, shader, "_LtcMatrixGgx", ggxLookupTable.matrixLUT);
		SetShaderProperty(s.commandBuffer, shader, "_LtcAmplitudeGgx", ggxLookupTable.amplitudeLUT);
		SetShaderProperty(s.commandBuffer, shader, "_Target", target);
		SetShaderProperty(s.commandBuffer, shader, "_Accelerator", accelerator);
		SetShaderProperty(s.commandBuffer, shader, "_LightSourceID", lightSourceID);
		SetShaderProperty(s.commandBuffer, shader, "_Center", transform.position);

		s.commandBuffer.SetRayTracingShaderPass(shader, "Raytracing");
		s.commandBuffer.DispatchRays(shader, "RayGeneration", (uint)camera.pixelWidth, (uint)camera.pixelHeight, 1, camera);
		s.commandBuffer.Blit(target, BuiltinRenderTextureType.CurrentActive, additiveBlit);
	}

	public override void DisposeLight() {
		Dispose(vertices);
		Dispose(nodes);
		Dispose(silhouettes);
	}

	public override void DisposeCamera(Camera camera, object state) {
		var s = (State)state;

		if (camera) camera.RemoveCommandBuffer(CameraEvent.AfterLighting, s.commandBuffer);

		Dispose(s.commandBuffer);
	}
}