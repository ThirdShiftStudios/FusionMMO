Shader "Hidden/Vista/Graph/PaintMaskNode"
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

	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	float _Falloff;
	float _Opacity;

	#define DT 1/30

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

	float brush(float2 uv)
	{
		float brush = coneShape(uv);
		float falloff = max(0.0001, _Falloff);
		brush = levels(brush, 0, falloff * 0.5, falloff, 0, _Opacity);
		return brush;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "Add"
			Blend One Zero
			BlendOp Add
			Cull Off

			CGPROGRAM

			float frag(v2f input): SV_Target
			{
				float currentValue = tex2D(_MainTex, input.localPos).r;
				float brushValue = brush(input.uv);
				float value = saturate(currentValue + brushValue*DT);

				return value;
			}
			ENDCG
		}

		Pass
		{
			Name "Sub"
			Blend One Zero
			BlendOp Add
			Cull Off

			CGPROGRAM

			float frag(v2f input): SV_Target
			{
				float currentValue = tex2D(_MainTex, input.localPos).r;
				float brushValue = brush(input.uv);
				float value = saturate(currentValue - brushValue*DT);

				return value;
			}
			ENDCG
		}

		Pass
		{
			Name "Smooth"
			Blend One Zero
			BlendOp Add
			Cull Off

			CGPROGRAM

			float getWeight(float x)
			{
				float standardDeviation = 0.3;
				float sqrSD = standardDeviation * standardDeviation;
				return exp(-x * x / (2 * sqrSD));
			}

			float blur(float2 uvPos, int radius)
			{
				float2 texelSize = _MainTex_TexelSize.xy;
				float2 textureRes = _MainTex_TexelSize.zw;
				float avgColor = 0;
				float sampleCount = 0;
				float2 uv = float2(0, 0);
				float2 centerPixel = uvPos * textureRes;
				float2 pixelPos = float2(0, 0);
				float d = 0;
				float f = 0;
				float weight = 0;
				for (int x0 = -radius; x0 <= radius; ++x0)
				{
					for (int y0 = -radius; y0 <= radius; ++y0)
					{
						uv = uvPos + float2(x0 * texelSize.x, y0 * texelSize.y);
						pixelPos = uv * textureRes;
						d = distance(centerPixel, pixelPos);
						f = saturate(d / radius);
						weight = getWeight(f);
						avgColor += tex2D(_MainTex, uv).r * weight;
						sampleCount += weight;
					}
				}
				avgColor = avgColor / sampleCount;
				return avgColor;
			}

			float frag(v2f input): SV_Target
			{
				float currentValue = tex2D(_MainTex, input.localPos).r;
				float blurValue = blur(input.localPos, 2);
				float brushValue = brush(input.uv);
				float value = lerp(currentValue, blurValue, brushValue);

				return value;
			}
			ENDCG

		}
	}
}
