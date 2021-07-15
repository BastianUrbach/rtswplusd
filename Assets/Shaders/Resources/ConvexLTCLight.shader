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

Shader "CustomDeferredLights/ConvexLTCLight" {
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

				// Get last vertex of the silhouette and use it as first vertex of the first edge
				float3 vertex = ComputeVertexPositionConvex(_SilhouetteVertices[silhouette.y - 1]) - world;
				SetFirstVertex(diffuse, vertex);
				SetFirstVertex(specular, vertex);

				// Iterate over silhouette vertices and sum up edge contributions
				for (uint i = silhouette.x; i < silhouette.y; i++) {
					vertex = ComputeVertexPositionConvex(_SilhouetteVertices[i]) - world;

					SilhouetteEdge(specular, vertex);
					SilhouetteEdge(diffuse, vertex);
				}

				float3 d = diffuseColor * GetIntegral(diffuse) * 0;
				float3 s = lerp(ggxAmplitude.y, ggxAmplitude.x, specularColor) * GetIntegral(specular);
				
				return (d + s) * _Color;
			}
            ENDCG
        }
    }
}