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

#if defined(SHADER_STAGE_FRAGMENT) | defined(SHADER_STAGE_VERTEX)

sampler2D _LtcMatrixGgx;
sampler2D _LtcAmplitudeGgx;

float3x3 GetLtcInverseTransformGgx(float roughness, float NdotV, out float2 amplitude) {
	const float size = 32;
	float2 uv = float2(roughness, acos(NdotV) / 3.14159 * 2);
	uv = (uv * (size - 1) + 0.5) / size;

	amplitude = tex2D(_LtcAmplitudeGgx, uv).rg;

	float4 v = tex2D(_LtcMatrixGgx, uv);

	return float3x3(
		1.0, 0.0, v.w,
        0.0, v.z, 0.0,
        v.y, 0.0, v.x
	);
}

float3x3 GetLtcInverseTransformLambert(float roughness, float NdotV) {
	return float3x3(
		1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0
	);
}

#else

Texture2D<float4> _LtcMatrixGgx;
Texture2D<float4> _LtcAmplitudeGgx;
SamplerState _LtcLutLinearClampSampler;

float3x3 GetLtcInverseTransformGgx(float roughness, float NdotV, out float2 amplitude) {
	const float size = 32;
	float2 uv = float2(roughness, acos(NdotV) / 3.14159 * 2);
	uv = (uv * (size - 1) + 0.5) / size;
	
	amplitude = _LtcAmplitudeGgx.SampleLevel(_LtcLutLinearClampSampler, uv, 0).rg;

	float4 v = _LtcMatrixGgx.SampleLevel(_LtcLutLinearClampSampler, uv, 0);

	return float3x3(
		1.0, 0.0, v.w,
        0.0, v.z, 0.0,
        v.y, 0.0, v.x
	);
}

float3x3 GetLtcInverseTransformLambert(float roughness, float NdotV) {
	return float3x3(
		1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0
	);
}

#endif