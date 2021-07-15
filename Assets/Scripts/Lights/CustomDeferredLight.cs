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

// Base class for custom deferred lights
public class CustomDeferredLight : MonoBehaviour {
	Dictionary<Camera, InternalState> states = new Dictionary<Camera, InternalState>();

	public virtual void InitializeLight() { }
	public virtual void UpdateLight() { }
	public virtual void DisposeLight() { }

	public virtual void InitializeCamera(Camera camera, out object state) => state = null;
	public virtual void UpdateCamera(Camera camera, ref object state) { }
	public virtual void DisposeCamera(Camera camera, object state) { }
	public virtual void UpdateCameraResolution(Camera camera, ref object state) { }

	struct InternalState {
		public int width;
		public int height;
		public object state;
	}

	void Start() {
		InitializeLight();
	}

	void Update() {
		UpdateLight();

		foreach (var pair in states) {
			if (!pair.Key) {
				DisposeCamera(pair.Key, pair.Value.state);
				states.Remove(pair.Key);
			}
		}

		foreach (var camera in Camera.allCameras) {
			if (!camera) continue;

			InternalState state;

			if (!states.TryGetValue(camera, out state)) {
				InitializeCamera(camera, out state.state);
			}

			if (state.width != camera.pixelWidth | state.height != camera.pixelHeight) {
				UpdateCameraResolution(camera, ref state.state);
				state.width = camera.pixelWidth;
				state.height = camera.pixelHeight;
			}

			UpdateCamera(camera, ref state.state);
			states[camera] = state;
		}
	}

	void OnDestroy() {
		foreach (var pair in states) {
			DisposeCamera(pair.Key, pair.Value.state);
		}

		DisposeLight();
	}
}