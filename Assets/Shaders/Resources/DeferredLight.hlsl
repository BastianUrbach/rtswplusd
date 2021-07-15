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

#include "UnityCG.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
sampler2D _CameraDepthTexture;

float3 Lighting(float3 world, float3 view, float3 albedo, float3 specular, float3 normal, float roughness);

float3 ComputeWorldRay(float2 uv) {
	float4 pos = float4(uv * 2 - 1, 1, 1);
	float3 ray = mul(unity_CameraInvProjection, pos).xyz;
	ray.z = -ray.z;
	return mul(unity_CameraToWorld, ray);
}

float3 ComputeWorldPos(float2 screenPos, float3 ray) {
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPos);
	depth = LinearEyeDepth(depth);
	return _WorldSpaceCameraPos + ray * depth;
}

void Vertex(
	in float4 vertex : POSITION,
	in float2 uv : TEXCOORD0,

	out float4 position : SV_POSITION,
	out float2 screenPos : TEXCOORD0,
	out float3 worldRay : TEXCOORD1
) {
	position = UnityObjectToClipPos(vertex);
	worldRay = ComputeWorldRay(uv);
	screenPos = uv;
}

void Fragment(
	in float4 position : SV_POSITION,
	in float2 screenPos : TEXCOORD0,
	in float3 worldRay : TEXCOORD1,

	out float4 target : SV_Target
) {
	float4 gbuffer0 = tex2D(_CameraGBufferTexture0, screenPos);
	float4 gbuffer1 = tex2D(_CameraGBufferTexture1, screenPos);
	float4 gbuffer2 = tex2D(_CameraGBufferTexture2, screenPos);

	float3 worldPos = ComputeWorldPos(screenPos, worldRay);

	target.a = 0;
	
	target.rgb = Lighting(
		worldPos,
		-normalize(worldRay),
		gbuffer0.rgb,
		gbuffer1.rgb,
		gbuffer2.xyz * 2 - 1,
		1 - gbuffer1.a
	);
}