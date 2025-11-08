Shader "Hidden/Vista/RealWorldData/Graph/HSLSelector"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Includes/RwdShaderIncludes.hlsl"
			#include "../Includes/ColorConversion.hlsl"

			struct appdata
			{
				float4 vertex: POSITION;
				float2 uv: TEXCOORD0;
			};

			struct v2f
			{
				float2 uv: TEXCOORD0;
				float4 vertex: SV_POSITION;
				float4 localPos: TEXCOORD1;
			};

			sampler2D _MainTex;
			float _MinH;
			float _MaxH;
			float _MinS;
			float _MaxS;
			float _MinL;
			float _MaxL;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.localPos = v.vertex;
				return o;
			}

			float frag(v2f input): SV_Target
			{
				float3 rgb = tex2D(_MainTex, input.localPos.xy).rgb;
				float3 hsl = RGBtoHSL(rgb);

				float vh = (hsl.x >= _MinH) * (hsl.x <= _MaxH);
				float vs = (hsl.y >= _MinS) * (hsl.y <= _MaxS);
				float vl = (hsl.z >= _MinL) * (hsl.z <= _MaxL);

				float v = vh * vs * vl;

				return v;
			}
			ENDCG

		}
	}
}
