Shader "WaterEffect/FlowMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Wave ("Wave", 2D) = "white" {}
		_FlowMap ("FlowMap", 2D) = "black" {}
		_Speed ("Speed", Float) = 1
		_FlowPower ("Flow Power", float) = 1.0
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
				float2 uvW : TEXCOORD1;
				float2 uvFM : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _Wave;
			sampler2D _FlowMap;
			float4 _MainTex_ST;
			float4 _Wave_ST;
			float4 _FlowMap_ST;
			float _Speed;
			float _FlowPower;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvW = TRANSFORM_TEX(v.uv, _Wave);
				o.uvFM = TRANSFORM_TEX(v.uv, _FlowMap);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float time = _Time.y * _Speed;
				
				float2 flow = tex2D(_FlowMap, i.uvFM).xy;
				flow = (flow * 2 -1) * _FlowPower;
				
				float time0 = frac(_Time.y * _Speed);
				float time1 = frac(_Time.y * _Speed  + 0.5);
				
				float2 uv0 = flow * time0;
				float2 uv1 = flow * time1;
				
				float lerpRate = abs((0.5 -  frac(_Time.y *_Speed)) * 2);
				
				fixed4 wave0 = tex2D(_Wave, i.uvW + uv0);
				fixed4 wave1 = tex2D(_Wave, i.uvW + uv1);
				
				fixed4 col0 = tex2D(_MainTex, i.uv + uv0) * (wave0 * 2);
				fixed4 col1 = tex2D(_MainTex, i.uv + uv1) * (wave1 * 2);
				
				fixed4 col = lerp(col0, col1, lerpRate);
				
				return col;
			}
			ENDCG
		}
	}
}
