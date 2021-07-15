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
using UnityEngine.Rendering;
using static Utility;

public class ConvexLTCLight : CustomDeferredLight {
    public ConvexSilhouetteBSP bsp;
	public LtcLookupTable ggxLookupTable;
	public Material material;

	[ColorUsage(false, true)]
	public Color color;

	Material lightMaterial;
	ComputeBuffer vertices;
	ComputeBuffer nodes;
	ComputeBuffer silhouettes;
	CommandBuffer commandBuffer;
	MaterialPropertyBlock materialProperties;

	public override void InitializeLight() {
		lightMaterial = new Material(Shader.Find("CustomDeferredLights/ConvexLTCLight"));

		vertices = bsp.GetVertexBuffer();
		nodes = bsp.GetNodeBuffer();
		silhouettes = bsp.GetSilhouetteBuffer();

		SetShaderProperty(lightMaterial, "_Vertices", vertices);
		SetShaderProperty(lightMaterial, "_Nodes", nodes);
		SetShaderProperty(lightMaterial, "_SilhouetteVertices", silhouettes);
		SetShaderProperty(lightMaterial, "_Root", bsp.root);
		SetShaderProperty(lightMaterial, "_LtcMatrixGgx", ggxLookupTable.matrixLUT);
		SetShaderProperty(lightMaterial, "_LtcAmplitudeGgx", ggxLookupTable.amplitudeLUT);

		materialProperties = new MaterialPropertyBlock();
		commandBuffer = new CommandBuffer();
	}

	public override void UpdateLight() {
		SetShaderProperty(lightMaterial, "_Color", color);
		SetShaderProperty(lightMaterial, "_LightToWorld", transform.localToWorldMatrix);
		SetShaderProperty(lightMaterial, "_WorldToLight", transform.worldToLocalMatrix);

		commandBuffer.Clear();
		Blit(commandBuffer, BuiltinRenderTextureType.CurrentActive, lightMaterial);

		if (material) {
			Color.RGBToHSV(color, out var h, out var s, out var v);
			SetShaderProperty(materialProperties, "_Color", Color.HSVToRGB(h, s, 1));
			Graphics.DrawMesh(bsp.mesh, transform.localToWorldMatrix, material, 0, null, 0, materialProperties);
		}
	}

	public override void DisposeLight() {
		Dispose(vertices);
		Dispose(nodes);
		Dispose(silhouettes);
	}

	public override void InitializeCamera(Camera camera, out object state) {
		camera.AddCommandBuffer(CameraEvent.AfterLighting, commandBuffer);
		state = null;
	}

	public override void DisposeCamera(Camera camera, object state) {
		if (camera) {
			camera.RemoveCommandBuffer(CameraEvent.AfterLighting, commandBuffer);
		}
	}
}