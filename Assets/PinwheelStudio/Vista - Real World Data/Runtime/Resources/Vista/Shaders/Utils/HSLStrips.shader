Shader "Hidden/Vista/RealWorldData/HSLStrips"
{
CGINCLUDE
#pragma vertex vert
#pragma fragment frag
#pragma shader_feature_local DRAW_H_STRIP
#pragma shader_feature_local DRAW_S_STRIP
#pragma shader_feature_local DRAW_L_STRIP
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
};

float _Hue;

v2f vert(appdata v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv = v.uv;
	return o;
}

ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 100
		Cull Off

		Pass
		{
			Name "Hue"
			CGPROGRAM

			float3 frag(v2f i): SV_Target
			{
				float3 hsl = 0;
				float3 rgb = 0;
				#if DRAW_H_STRIP
					hsl = float3(i.uv.x, 1, 0.5);
					rgb = HSLtoRGB(hsl);
				#endif
				#if DRAW_S_STRIP
					hsl = float3(_Hue, i.uv.x, 0.5);
					rgb = HSLtoRGB(hsl);
				#endif
				#if DRAW_L_STRIP
					hsl = float3(_Hue, 1, i.uv.x);
					rgb = HSLtoRGB(hsl);
				#endif

				return rgb;
			}
			ENDCG

		}
	}
}
