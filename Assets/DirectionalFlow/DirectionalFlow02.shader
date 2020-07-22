Shader "WaterEffect/DirectionalFlow02"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FlowMap ("FlowMap", 2D) = "black" {}
		_Tiling ("Tiling", Float) = 1
		_GridResolution ("Grid Resolution", Float) = 10
		_Speed ("Speed", Float) = 1
		_FlowStrength ("Flow Strength", Float) = 1
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
			float _FlowStrength;
			float _GridResolution;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvFM = TRANSFORM_TEX(v.uv, _FlowMap);
				return o;
			}
			
			float2 DirectionalFlowUV (float2 uv, float3 dir, float tiling, float time) {
				float2 d = normalize(dir.xy);
				uv = mul(float2x2(d.y, -d.x, d.x, d.y), uv);
				uv.y -= time * dir.z;
				//uv.y -= time * length(uv.xy);
				return uv * tiling;
			}
			
			float3 GetFlow(float2 uv) {
				float2 uvFM = uv;
				
				uvFM = (floor(uvFM * _GridResolution)) 
					/ _GridResolution;
				
				float3 flow0 = tex2D(_FlowMap, uvFM).xyz  * 2 - 1;
				float3 flow1 = tex2D(_FlowMap, uvFM + float2(1,0)).xyz  * 2 - 1;
				float3 flow2 = tex2D(_FlowMap, uvFM + float2(0,1)).xyz  * 2 - 1;
				float3 flow3 = tex2D(_FlowMap, uvFM + float2(1,1)).xyz  * 2 - 1;
				
				float2 t = uv * _GridResolution;
				t = abs(2 * frac(t) - 1);
				
				float wA = (1 - t.x) * (1 - t.y);
				float wB = t.x * (1 - t.y);
				float wC = (1 - t.x) * t.y;
				float wD = t.x * t.y;
				
				//flow = flow * 2 - 1;
				
				return flow0 * wA + flow1 * wB + flow2 * wC + flow3 * wD;
			}
			
			
			
			
			fixed4 frag (v2f i) : SV_Target
			{
				
				float3 flow = GetFlow(i.uvFM);
				flow.z *= _FlowStrength;
				
				float tiling = flow.z * _Tiling;
				//float tiling = _Tiling;
				
				float time0 = frac(_Time.y * _Speed);
				float time1 = frac((_Time.y + 0.5) * _Speed);
				
				float2 uv0 = DirectionalFlowUV(i.uv, flow, tiling, time0);
				float2 uv1 = DirectionalFlowUV(i.uv, flow, tiling, time1);
				
				float lerpRate = abs((0.5 -  frac(_Time.y * 0.25))/ 0.5);
				
				fixed4 col0 = tex2D(_MainTex, uv0);
				fixed4 col1 = tex2D(_MainTex, uv1);
				
				//col0.gb = 1;
				//col1.rb = 1;
				
				fixed4 col = lerp(col0, col1, lerpRate);
				
				//fixed4 col = col0 * col1;
				
				//fixed4 col = col0;
				//fixed4 col = col1;
				
				col.rgb = lerp(1, col.rgb, length(flow.xy) - 0.5);
				return col;
			}
			ENDCG
		}
	}
}
