Shader "Unlit/Slot"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Background ("Background", 2D) = "white" {}
		_Rust ("Rust", 2D) = "white" {}
		_Alpha ("Alpha", 2D) = "white" {}
		_Force ("Force", float) = 0
		_SpinIndex ("SpinIndex", float) = 0
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _Background;
			float4 _Background_ST;

			sampler2D _Alpha;
			sampler2D _Rust;

			float _Force;
			float _SpinIndex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _Background);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv0;
				uv.y = (uv.y - _Force) + _SpinIndex;

				fixed4 col = tex2D(_MainTex, uv);
				fixed4 alpha = tex2D(_Alpha, uv);

				fixed4 rust = tex2D(_Rust, uv);

				for (float a = 0; a < 12; a++)
				{
					float2 uv2 = uv;
					uv2.y += ((a - 4) * (-_Force * 0.001));
					col += tex2D(_MainTex, uv2);
				}
				col /= 12;

				fixed4 bg = tex2D(_Background, i.uv1) * rust;

				fixed4 sum = col + (alpha * 5 * (_Force * 0.01));
				sum = lerp(bg, sum, col.a) * rust;

				return sum;
			}
			ENDCG
		}
	}
}
