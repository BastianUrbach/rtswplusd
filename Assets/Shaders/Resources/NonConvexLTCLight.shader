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

Shader "CustomDeferredLights/NonConvexLTCLight" {
    SubShader {
        Cull Off
		ZWrite Off
		ZTest Always
		Blend One One

        Pass {
            CGPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "DeferredLight.hlsl"
			#include "LtcCommon.hlsl"
			#include "LtcLut.hlsl"
			#include "BspCommon.hlsl"

			float4 _Color;

			float3 Lighting(float3 world, float3 view, float3 diffuseColor, float3 specularColor, float3 normal, float roughness) {
				float3 localPosition = WorldToLightPos(world);
				float NdotV = dot(normal, view);
				float3x3 shadingSpace = BuildShadingSpace(normal, view);

				float2 ggxAmplitude;
				
				LTC specular = NewLTC(GetLtcInverseTransformGgx(roughness, NdotV, ggxAmplitude), shadingSpace);
				LTC diffuse = NewLTC(GetLtcInverseTransformLambert(roughness, NdotV), shadingSpace);

				// Find silhouette start and end indices
				uint2 silhouette = FindSilhouette(localPosition);

				// Iterate over silhouette vertices and sum up edge contributions
				for (uint i = silhouette.x; i < silhouette.y; i++) {
					uint2 edge = _SilhouetteEdges[i];
					
					float3 a = ComputeVertexPositionNonConvex(edge.x, localPosition) - world;
					float3 b = ComputeVertexPositionNonConvex(edge.y, localPosition) - world;

					SilhouetteEdge(specular, a, b);
					SilhouetteEdge(diffuse, a, b);
				}

				float3 d = GetIntegral(diffuse) * diffuseColor;
				float3 s = GetIntegral(specular) * lerp(ggxAmplitude.y, ggxAmplitude.x, specularColor);
				
				return (d + s) * _Color;
			}
            ENDCG
        }
    }
}