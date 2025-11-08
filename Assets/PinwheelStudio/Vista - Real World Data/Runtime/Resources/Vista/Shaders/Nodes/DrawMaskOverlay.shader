Shader "Hidden/Vista/RealWorldData/Graph/DrawMaskOverlay"
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

			sampler2D _ColorMap;
			sampler2D _MaskMap;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.localPos = v.vertex;
				return o;
			}

			float4 frag(v2f input): SV_Target
			{
				float3 color = tex2D(_ColorMap, input.localPos.xy).rgb;
				float3 mask = float3(tex2D(_MaskMap, input.localPos.xy).r, 0, 0);
				float3 v = lerp(color, mask, mask.r);
				return float4(v, 1);
			}
			ENDCG

		}
	}
}
