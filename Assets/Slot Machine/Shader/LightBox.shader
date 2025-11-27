Shader "Unlit/Slot"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Force ("Force", float) = 0
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
			};

			struct v2f
			{
				float2 uv0 : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _Force;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float t = _Time * 10;
				float2 uv = i.uv0;

				t = floor(sin(t) * 20) / 3;

				uv.y = uv.y + t;

				fixed4 col = tex2D(_MainTex, uv);

				for (float a = 0; a < 12; a++)
				{
					float2 uv2 = uv;
					uv2.y += ((a - 4) * (-_Force * 0.0001));
					col += tex2D(_MainTex, uv2);
				}
				col /= 12;

				return col;
			}
			ENDCG
		}
	}
}
