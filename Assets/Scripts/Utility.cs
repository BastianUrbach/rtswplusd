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
using static System.Runtime.InteropServices.Marshal;

public class Utility {
	public const double epsilon = 0.00001;

	public static void Swap<T>(ref T a, ref T b) {
		T temp = a;
		a = b;
		b = temp;
	}

	// Automated waste separation
	public static void Dispose(Object o) {
		if (!o) return;

		if (Application.isPlaying) {
			Object.Destroy(o);
		} else {
			Object.DestroyImmediate(o);
		}
	}

	public static void Dispose(ComputeBuffer cb) {
		if (cb != null) cb.Release();
	}

	public static void Dispose(CommandBuffer cb) {
		if (cb != null) cb.Release();
	}

	public static void Dispose(RenderTexture rt) {
		if (rt != null) rt.Release();
	}

	// True if Gizmos should be rendered
	public static bool GizmosEnabled() {
		#if UNITY_EDITOR
		return UnityEditor.Handles.ShouldRenderGizmos();
		#else
		return false;
		#endif
	}

	static Texture2D dummyTexture;

	static Texture2D GetDummyTexture() {
		if (dummyTexture == null) {
			dummyTexture = new Texture2D(1, 1);
		}

		return dummyTexture;
	}

	public static void Blit(CommandBuffer commandBuffer, RenderTargetIdentifier target, Material material) {
		commandBuffer.Blit(GetDummyTexture(), target, material);
	}

	public static void Blit(CommandBuffer commandBuffer, RenderTargetIdentifier target, Material material, int pass) {
		commandBuffer.Blit(GetDummyTexture(), target, material, pass);
	}

	public static void Blit(RenderTexture target, Material material) {
		Graphics.Blit(GetDummyTexture(), target, material);
	}

	public static void Blit(RenderTexture target, Material material, int pass) {
		Graphics.Blit(GetDummyTexture(), target, material, pass);
	}

	public static ComputeBuffer Buffer<T>(T[] data, ComputeBufferType type = ComputeBufferType.Structured, ComputeBufferMode mode = ComputeBufferMode.Immutable) {
		var buffer = new ComputeBuffer(data.Length, SizeOf<T>(), type, mode);
		buffer.SetData(data);
		return buffer;
	}

	public static double Clamp(double x, double min, double max) {
		return x < min ? min : x > max ? max : x;
	}

	public static double DivideSafe(double a, double b) {
		if (b < epsilon && b > -epsilon) {
			b = b < 0 ? -epsilon : epsilon;
		}

		return a / b;
	}

	public static void SetShaderProperty(string name, float value)         => Shader.SetGlobalFloat(name, value);
	public static void SetShaderProperty(string name, int value)           => Shader.SetGlobalInteger(name, value);
	public static void SetShaderProperty(string name, Vector4 value)       => Shader.SetGlobalVector(name, value);
	public static void SetShaderProperty(string name, Color value)         => Shader.SetGlobalColor(name, value);
	public static void SetShaderProperty(string name, Matrix4x4 value)     => Shader.SetGlobalMatrix(name, value);
	public static void SetShaderProperty(string name, Texture value)       => Shader.SetGlobalTexture(name, value);
	public static void SetShaderProperty(string name, Vector4[] value)     => Shader.SetGlobalVectorArray(name, value);
	public static void SetShaderProperty(string name, Matrix4x4[] value)   => Shader.SetGlobalMatrixArray(name, value);
	public static void SetShaderProperty(string name, ComputeBuffer value) => Shader.SetGlobalBuffer(name, value);
	
	public static void SetShaderProperty(Material material, string name, float value)         => material.SetFloat(name, value);
	public static void SetShaderProperty(Material material, string name, int value)           => material.SetInteger(name, value);
	public static void SetShaderProperty(Material material, string name, Vector4 value)       => material.SetVector(name, value);
	public static void SetShaderProperty(Material material, string name, Color value)         => material.SetColor(name, value);
	public static void SetShaderProperty(Material material, string name, Matrix4x4 value)     => material.SetMatrix(name, value);
	public static void SetShaderProperty(Material material, string name, Texture value)       => material.SetTexture(name, value);
	public static void SetShaderProperty(Material material, string name, Vector4[] value)     => material.SetVectorArray(name, value);
	public static void SetShaderProperty(Material material, string name, Matrix4x4[] value)   => material.SetMatrixArray(name, value);
	public static void SetShaderProperty(Material material, string name, ComputeBuffer value) => material.SetBuffer(name, value);

	public static void SetShaderProperty(MaterialPropertyBlock block, string name, float value)         => block.SetFloat(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, int value)           => block.SetInteger(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Vector4 value)       => block.SetVector(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Color value)         => block.SetColor(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Matrix4x4 value)     => block.SetMatrix(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Texture value)       => block.SetTexture(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Vector4[] value)     => block.SetVectorArray(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, Matrix4x4[] value)   => block.SetMatrixArray(name, value);
	public static void SetShaderProperty(MaterialPropertyBlock block, string name, ComputeBuffer value) => block.SetBuffer(name, value);
	
	public static void SetShaderProperty(RayTracingShader shader, string name, float value)         => shader.SetFloat(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, int value)           => shader.SetInt(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Vector4 value)       => shader.SetVector(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Color value)         => shader.SetVector(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Matrix4x4 value)     => shader.SetMatrix(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Texture value)       => shader.SetTexture(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Vector4[] value)     => shader.SetVectorArray(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, Matrix4x4[] value)   => shader.SetMatrixArray(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, ComputeBuffer value) => shader.SetBuffer(name, value);
	public static void SetShaderProperty(RayTracingShader shader, string name, RayTracingAccelerationStructure value) => shader.SetAccelerationStructure(name, value);

	public static void SetShaderProperty(CommandBuffer buffer, string name, float value)         => buffer.SetGlobalFloat(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, int value)           => buffer.SetGlobalInt(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Vector4 value)       => buffer.SetGlobalVector(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Color value)         => buffer.SetGlobalColor(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Matrix4x4 value)     => buffer.SetGlobalMatrix(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Texture value)       => buffer.SetGlobalTexture(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Vector4[] value)     => buffer.SetGlobalVectorArray(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, Matrix4x4[] value)   => buffer.SetGlobalMatrixArray(name, value);
	public static void SetShaderProperty(CommandBuffer buffer, string name, ComputeBuffer value) => buffer.SetGlobalBuffer(name, value);

	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, int value)           => buffer.SetRayTracingIntParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Vector4 value)       => buffer.SetRayTracingVectorParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, float value)         => buffer.SetRayTracingFloatParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Color value)         => buffer.SetRayTracingVectorParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Matrix4x4 value)     => buffer.SetRayTracingMatrixParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Texture value)       => buffer.SetRayTracingTextureParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Vector4[] value)     => buffer.SetRayTracingVectorArrayParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, Matrix4x4[] value)   => buffer.SetRayTracingMatrixArrayParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, ComputeBuffer value) => buffer.SetRayTracingBufferParam(shader, name, value);
	public static void SetShaderProperty(CommandBuffer buffer, RayTracingShader shader, string name, RayTracingAccelerationStructure value) => buffer.SetRayTracingAccelerationStructure(shader, name, value);
}