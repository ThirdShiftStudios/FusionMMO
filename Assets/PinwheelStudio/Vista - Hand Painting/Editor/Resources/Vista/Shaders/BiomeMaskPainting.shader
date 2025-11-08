Shader "Hidden/Vista/BiomeMaskPainting"
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

	sampler2D _BaseBiomeMask;
	sampler2D _AdjustmentMap;
	float4 _TexelSize;
	float _Falloff;
	float _Opacity;
	float _Multiplier;

	#define DT 1.0 / 30.0

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.localPos = v.vertex;
		return o;
	}

	float coneShape(float2 uv)
	{
		uv = uv * 2 - 1;
		float d = length(uv);
		float f = saturate(1 - d);
		return f;
	}
			
	float inverseLerp(float value, float a, float b)
	{
		float v = (value - a) / (b - a);
		float aeb = (a == b);
		return 0 * aeb + v * (1 - aeb);
	}

	float levels(float v, float inLow, float inMid, float inHigh, float outLow, float outHigh)
	{
		if (v <= inMid)
		{
			v = inverseLerp(v, inLow, 2 * inMid - inLow);
		}
		else
		{
			v = inverseLerp(v, 2 * inMid - inHigh, inHigh);
		}

		v = lerp(outLow, outHigh, saturate(v));
		return v;
	}
	ENDCG
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "Add Sub"
			CGPROGRAM			

			float frag(v2f input): SV_Target
			{
				float baseMask = tex2D(_BaseBiomeMask, input.localPos.xy).r;

				float brush = coneShape(input.uv);
				float falloff = max(0.0001, _Falloff);
				brush = levels(brush, 0, falloff * 0.5, falloff, 0, _Opacity);
				brush *= _Multiplier*DT;

				float currentAdjustment = tex2D(_AdjustmentMap, input.localPos.xy).r;
				float newAdjustment = currentAdjustment + brush;

				float maskSum = baseMask + newAdjustment;
				maskSum = clamp(maskSum, 0, 1);

				float value = maskSum - baseMask;
				return value;
			}
			ENDCG
		}

		Pass
		{
			Name "Add Sub"
			CGPROGRAM			

			float getWeight(float x)
			{
				float standardDeviation = 0.3;
				float sqrSD = standardDeviation * standardDeviation;
				return exp(-x * x / (2 * sqrSD));
			}

			float frag(v2f input): SV_Target
			{
				float brush = coneShape(input.uv);
				float falloff = max(0.0001, _Falloff);
				brush = levels(brush, 0, falloff * 0.5, falloff, 0, _Opacity);

				float2 texelSize = _TexelSize.xy;
				float2 textureRes = _TexelSize.zw;
				float avgColor = 0;
				float sampleCount = 0;
				float2 uv = float2(0,0);
				float2 centerPixel = input.uv * textureRes;
				float2 pixelPos = float2(0, 0);
				float d = 0;
				float f = 0;
				float weight = 0; 
				int radius = 3;
				for (int x0 = -radius; x0 <= radius; ++x0)
				{
					for (int y0 = -radius; y0 <= radius; ++y0)
					{
						uv = input.localPos.xy + float2(x0 * texelSize.x, y0 * texelSize.y); 
						pixelPos = uv * textureRes; 
						d = distance(centerPixel, pixelPos); 
						f = saturate(d / radius);
						weight = getWeight(f);
						avgColor += saturate(tex2D(_BaseBiomeMask, uv).r + tex2D(_AdjustmentMap, uv).r) * weight;
						sampleCount += weight;
					}
				}
				avgColor = avgColor/sampleCount;

				float baseMask = tex2D(_BaseBiomeMask, input.localPos.xy).r;
				float adjustment = tex2D(_AdjustmentMap, input.localPos.xy).r;
				float maskSum = saturate(baseMask + adjustment);
				maskSum = lerp(maskSum, avgColor, brush);

				float value = maskSum - baseMask;
				return value;
			}
			ENDCG
		}
	}
}
