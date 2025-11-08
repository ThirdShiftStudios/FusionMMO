Shader "Hidden/Vista/RealWorldData/LoadRealWorldTextures"
{
CGINCLUDE
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "../Includes/RwdShaderIncludes.hlsl"
	#include MATH_HLSL

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

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.localPos = v.vertex;
		return o;
	}
ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "Output Height Map"
			CGPROGRAM

			float _RemapMinHeight;
			float _RemapMaxHeight;
			float _GraphMaxHeight;

			float frag(v2f input): SV_Target
			{
				float v = tex2D(_MainTex, input.localPos).r;
				float heightMeters = lerp(_RemapMinHeight, _RemapMaxHeight, v);
				float height01 = inverseLerp(heightMeters, 0, _GraphMaxHeight);
				return height01;
			}
			ENDCG
		}
	}
}
