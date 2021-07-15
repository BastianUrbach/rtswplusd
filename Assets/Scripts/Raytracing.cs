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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static Utility;

// Builds and stores the RayTracingAccelerationStructure used for GPU ray tracing and provides easy
// access to random write buffers for each camera.
public class Raytracing : MonoBehaviour {
	public LayerMask layers;
	public RayTracingAccelerationStructure.RayTracingModeMask modeMask;
	static RayTracingAccelerationStructure accelerator;

	static Dictionary<Camera, RenderTexture> renderTargets;

	void Start() {
		var settings = new RayTracingAccelerationStructure.RASSettings();
		settings.layerMask = layers;
		settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
		settings.rayTracingModeMask = modeMask;
		accelerator = new RayTracingAccelerationStructure(settings);
		accelerator.Build();

		renderTargets = new Dictionary<Camera, RenderTexture>();
	}

	public static RayTracingAccelerationStructure GetAccelerator() {
		return accelerator;
	}

	void OnDestroy() {
		accelerator.Release();
	}

	void Update() {
		foreach (var pair in renderTargets) {
			if (!pair.Key) {
				Dispose(pair.Value);
				renderTargets.Remove(pair.Key);
			}
		}
	}

	public static RenderTexture GetTarget(Camera camera) {
		if (!camera) return null;

		if (
			!renderTargets.TryGetValue(camera, out var renderTarget) ||
			renderTarget.width != camera.pixelWidth ||
			renderTarget.height != camera.pixelHeight
		) {
			Dispose(renderTarget);
			renderTarget = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, DefaultFormat.HDR);
			renderTarget.enableRandomWrite = true;
			renderTarget.Create();
			renderTargets[camera] = renderTarget;
		}

		return renderTarget;
	}
}