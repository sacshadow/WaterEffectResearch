Shader "WaterEffect/DirectionalFlow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FlowMap ("FlowMap", 2D) = "black" {}
		_Tiling ("Tiling", Float) = 1
		_GridResolution ("Grid Resolution", Float) = 10
		_Speed ("Speed", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uvFM : TEXCOORD1;

				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _FlowMap;
			float4 _MainTex_ST;
			float4 _FlowMap_ST;
			float _Speed;
			float _Tiling;
			float _GridResolution;
			
			float2 DirectionalFlowUV(float2 uv, float3 dir, float tiling, float time) {
				float2 d = normalize(dir.xy);
				uv = mul(float2x2(d.y, -d.x, d.x, d.y), uv);
				uv.y -= time * dir.z;
				return uv * tiling;
			}
			
			float3 FlowCell (float2 uv, float2 uvFM, float2 offset, float time, float gridB) {
				float2 shift = 1 - offset;
				shift *= 0.5;
				offset *= 0.5;
				
				if(gridB) {
					offset += 0.25;
					shift -= 0.25;
				}
				
				uvFM = (floor(uvFM * _GridResolution + offset) + shift) 
					/ _GridResolution;
				
				float3 flow = tex2D(_FlowMap, uvFM).rgb;
				flow = flow * 2 - 1;
				
				float tiling = flow.z * _Tiling;
				//float tiling = _Tiling;
				uv = DirectionalFlowUV(uv + offset, flow, tiling, time);
				//uv.y *= flow.z;
				//uv.x /= flow.z;
				
				fixed4 col = tex2D(_MainTex, uv);
				
				return col.rgb;
			}
			
			float3 FlowGrid(float2 uv, float2 uvFM, float time, bool gridB) {
				float3 dhA = FlowCell(uv, uvFM, float2(0,0), time, gridB);
				float3 dhB = FlowCell(uv, uvFM, float2(1,0), time, gridB);
				float3 dhC = FlowCell(uv, uvFM, float2(0,1), time, gridB);
				float3 dhD = FlowCell(uv, uvFM, float2(1,1), time, gridB);
				
				
				float2 t = abs(2 * frac(uvFM * _GridResolution) -1);
				
				if(gridB) {
					t += 0.25;
				}
				
				t = abs(2 * frac(t) - 1);
				
				float wA = (1 - t.x) * (1 - t.y);
				float wB = t.x * (1 - t.y);
				float wC = (1 - t.x) * t.y;
				float wD = t.x * t.y;
				
				return dhA * wA + dhB * wB + dhC * wC + dhD * wD;
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvFM = TRANSFORM_TEX(v.uv, _FlowMap);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float time = _Time.y * _Speed;
				
				float2 uv = i.uv;
				float2 uvFM = i.uvFM;
				
				
				float3 dh = FlowGrid(uv, uvFM, time, false);
				
				dh = (dh + FlowGrid(uv, uvFM, time, true)) * 0.5;
				
				float4 col = 1;
				col.rgb = dh.rgb;
				
				return col;
			}
			ENDCG
		}
	}
}
