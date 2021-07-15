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

#pragma max_recursion_depth 1

Texture2D<float4> _CameraGBufferTexture0;
Texture2D<float4> _CameraGBufferTexture1;
Texture2D<float4> _CameraGBufferTexture2;
Texture2D<float> _CameraDepthTexture;
RWTexture2D<float4> _Target;

SamplerState _PointClampSampler;

float4x4 unity_CameraInvProjection;
float4x4 unity_CameraToWorld;
float4 _ZBufferParams;
float3 _WorldSpaceCameraPos;
float4 _ScreenParams;

RaytracingAccelerationStructure _Accelerator;
uint _LightSourceID;

float3 Lighting(float3 world, float3 view, float3 albedo, float3 specular, float3 normal, float roughness, uint seed);

float LinearEyeDepth(float z) {
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}

float3 ComputeWorldRay(float2 uv) {
	float4 pos = float4(uv * 2 - 1, 1, 1);
	float3 ray = mul(unity_CameraInvProjection, pos).xyz;
	ray.z = -ray.z;
	return mul((float3x3)unity_CameraToWorld, ray);
}

float3 ComputeWorldPos(float2 screenPos, float3 ray) {
	float depth = _CameraDepthTexture.SampleLevel(_PointClampSampler, screenPos, 0).r;
	depth = LinearEyeDepth(depth);
	return _WorldSpaceCameraPos + ray * depth;
}

struct Payload { int lightSourceID; };

bool IsShadowed(float3 origin, float3 direction) {
	Payload p;
	p.lightSourceID = -1;

	RayDesc r;
	r.Origin = origin;
	r.Direction = direction;
	r.TMin = 0;
	r.TMax = 1000;

	TraceRay(_Accelerator, 0, 0xff, 0, 1, 0, r, p);

	return p.lightSourceID != _LightSourceID;
}

uint Hash(uint seed) {
	seed = (seed ^ 61) ^ (seed >> 16);
	seed *= 9;
	seed = seed ^ (seed >> 4);
	seed *= 0x27d4eb2d;
	seed = seed ^ (seed >> 15);
	return seed;
}

float Random(inout uint seed) {
	seed ^= (seed << 13);
	seed ^= (seed >> 17);
	seed ^= (seed << 5);
	return ((float)seed / 4294967296.0);
}

[shader("raygeneration")]
void RayGeneration() {
    uint2 id = DispatchRaysIndex().xy;
	float2 uv = id / _ScreenParams.xy;
	uint seed = Hash(id.x ^ (id.y << 16));

	float3 worldRay = ComputeWorldRay(uv);
	float3 worldPos = ComputeWorldPos(uv, worldRay);

	float4 gbuffer0 = _CameraGBufferTexture0.SampleLevel(_PointClampSampler, uv, 0);
	float4 gbuffer1 = _CameraGBufferTexture1.SampleLevel(_PointClampSampler, uv, 0);
	float4 gbuffer2 = _CameraGBufferTexture2.SampleLevel(_PointClampSampler, uv, 0);

	float4 target;

	target.a = 0;
	
	target.rgb = Lighting(
		worldPos,
		-normalize(worldRay),
		gbuffer0.rgb,
		gbuffer1.rgb,
		gbuffer2.xyz * 2 - 1,
		1 - gbuffer1.a,
		seed
	);

    _Target[id] = target;
}