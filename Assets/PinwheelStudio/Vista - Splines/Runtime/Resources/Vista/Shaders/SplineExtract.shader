Shader "Hidden/Vista/SplineExtract"
{
	CGINCLUDE
	#pragma vertex vert
	#pragma fragment frag
	#pragma require compute
	#pragma target 4.5

	#include "UnityCG.cginc"

	struct v2f
	{
		float4 vertex: SV_POSITION;
		float alpha: TEXCOORD0;
		float4 localPos: TEXCOORD1;
		float height: TEXCOORD2;
	};
			
	StructuredBuffer<float3> _Vertices; //world(x,y,z)
	StructuredBuffer<float> _Alphas;
	float4 _WorldBounds; //(x, y, width, height)
	float2 _TextureSize;
	float _MaxHeight;

	float inverseLerp(float value, float a, float b)
	{
		float v = (value - a) / (b - a);
		float aeb = (a == b);
		return 0 * aeb + v * (1 - aeb);
	}

	v2f vert(uint id: SV_VERTEXID)
	{
		v2f o;
		float3 v = _Vertices[id];
		float x = inverseLerp(v.x, _WorldBounds.x, _WorldBounds.x + _WorldBounds.z);
		float y = inverseLerp(v.z, _WorldBounds.y, _WorldBounds.y + _WorldBounds.w);
		o.vertex =  UnityObjectToClipPos(float4(x, y, 0, 1));
		o.alpha = _Alphas[id];
		o.localPos = float4(x, y, v.y, 1);
		o.height = saturate(v.y / _MaxHeight);
		return o;
	}
	ENDCG

		SubShader
	{
		Tags { "RenderType" = "Opaque"}
		LOD 100
		Cull Off
		Pass
		{
			Name "Render Depth"
			ZWrite On
			ZTest LEqual

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				return 0;
			}
			ENDCG

		}
		Pass
		{
			Name "Render Mask"
			ZWrite Off
			ZTest LEqual
		
			BlendOp Max
			Blend One One

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				return input.alpha;
			}
			ENDCG
		}
		Pass
		{
			Name "Render Mask Bool"

			CGPROGRAM

			float frag(v2f input): SV_Target
			{
				return 1;
			}
			ENDCG
		}
		Pass
		{
			Name "Render Height Map"
			BlendOp Max
			Blend One One

			CGPROGRAM

			float frag(v2f input): SV_Target
			{
				return input.height;
			}
			ENDCG
		}

	}
}
