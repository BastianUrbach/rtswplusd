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

Shader "Unlit/LightSourceUnlit" {
    Properties {
        _Color ("Color", Color) = (1, 1, 1, 1)
		_LightSourceID ("Light source ID", Int) = 0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

			float4 _Color;

            float4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }

	SubShader {
		Tags { "LightMode"="Raytracing" }

		Pass {
			Name "Raytracing"

			HLSLPROGRAM
			#pragma raytracing ClosestHit

			int _LightSourceID;

			struct Payload { int lightSourceID; };
			struct AttributeData { float2 barycentrics; };

			[shader("closesthit")]
			void ClosestHit(inout Payload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes) {
				payload.lightSourceID = _LightSourceID;
			}
			ENDHLSL
		}
	}
}
