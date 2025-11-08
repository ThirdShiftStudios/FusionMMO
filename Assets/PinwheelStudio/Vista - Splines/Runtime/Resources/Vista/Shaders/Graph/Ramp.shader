Shader "Hidden/Vista/Graph/Ramp"
{
	CGINCLUDE
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

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

	sampler2D _TerrainHeightMap;
	sampler2D _FalloffDetailMap;
	sampler2D _SplineFalloffMask;
	sampler2D _FalloffCurveMap;
	sampler2D _SplineHeightMap;

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
			Name "Height Output"
			CGPROGRAM
			float frag(v2f input): SV_Target
			{
				float2 uv = input.localPos.xy;
				float terrainHeight = tex2D(_TerrainHeightMap, uv).r;
				float splineHeight = tex2D(_SplineHeightMap, uv).r;
				float falloffDetail = tex2D(_FalloffDetailMap, uv).r;
				float splineFalloff = tex2D(_SplineFalloffMask, uv).r;
				splineFalloff = tex2D(_FalloffCurveMap, splineFalloff.xx).r;

				float detailExponent = lerp(1.9, 0.1, splineFalloff);
				falloffDetail = pow(falloffDetail, detailExponent);

				float d0 = saturate(splineFalloff * falloffDetail);
				float d1 = saturate(splineFalloff + falloffDetail);
				splineFalloff = lerp(d0, d1, splineFalloff);

				float h = lerp(terrainHeight, splineHeight, splineFalloff);

				return h;
			}
			ENDCG
		}
		Pass
		{
			Name "Ramp Mask Output"
			CGPROGRAM
			float frag(v2f input): SV_Target
			{
				float2 uv = input.localPos.xy;
				float falloffDetail = tex2D(_FalloffDetailMap, uv).r;
				float splineFalloff = tex2D(_SplineFalloffMask, uv).r;
				splineFalloff = tex2D(_FalloffCurveMap, splineFalloff.xx).r;

				float detailExponent = lerp(1.9, 0.1, splineFalloff);
				falloffDetail = pow(falloffDetail, detailExponent);

				float d0 = saturate(splineFalloff * falloffDetail);
				float d1 = saturate(splineFalloff + falloffDetail);
				splineFalloff = lerp(d0, d1, splineFalloff);

				return splineFalloff;
			}
			ENDCG
		}
	}
}
