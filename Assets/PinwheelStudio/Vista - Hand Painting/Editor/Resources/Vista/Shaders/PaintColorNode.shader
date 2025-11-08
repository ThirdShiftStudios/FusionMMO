Shader "Hidden/Vista/Graph/PaintColorNode"
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
	float4 _Color;

	#define DT 1 / 30

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
		brush = levels(brush, 0, falloff * 0.5, falloff, 0, 1);
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

			float4 frag(v2f input): SV_Target
			{
				float4 currentColor = tex2D(_MainTex, input.localPos);
				float4 brushColor = float4(_Color.rgb, _Color.a * brush(input.uv));
				
				float4 color = brushColor * brushColor.a + currentColor * (1 - brushColor.a);
				float alpha = 1 - (1 - brushColor.a) * (1 - currentColor.a);
				return float4(color.rgb, 1);
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

			float4 frag(v2f input): SV_Target
			{
				float4 currentColor = tex2D(_MainTex, input.localPos);
				float4 brushColor = float4(0,0,0,_Color.a * brush(input.uv));
				float4 color = brushColor * brushColor.a + currentColor * (1 - brushColor.a);
				return float4(color.rgb, 1);
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

			float4 blur(float2 uvPos, int radius)
			{
				float2 texelSize = _MainTex_TexelSize.xy;
				float2 textureRes = _MainTex_TexelSize.zw;
				float4 avgColor = 0;
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
						avgColor += tex2D(_MainTex, uv) * weight;
						sampleCount += weight;
					}
				}
				avgColor = avgColor / sampleCount;
				return avgColor;
			}

			float4 frag(v2f input): SV_Target
			{
				float4 currentValue = tex2D(_MainTex, input.localPos);
				float4 blurValue = blur(input.localPos, 5);
				float brushValue = brush(input.uv);
				float4 value = lerp(currentValue, blurValue, brushValue);

				return value;
			}
			ENDCG

		}
	}
}
