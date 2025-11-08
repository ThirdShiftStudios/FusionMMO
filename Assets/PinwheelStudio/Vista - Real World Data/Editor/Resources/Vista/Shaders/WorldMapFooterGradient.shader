Shader "Hidden/Vista/RealWorldData/WorldMapWindow/FooterGradient"
{
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

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

			#define START_COLOR float4(0, 0, 0, 0.75)
			#define END_COLOR float4(0, 0, 0, 0)

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i): SV_Target
			{
				float fx = sqrt(i.uv.y);
				float fy = pow(abs(i.uv.x * 2 - 1), 0.5);
				fixed4 color = lerp(START_COLOR, END_COLOR, fx * fx) * fy * fy;
				return color;
			}
			ENDCG

		}
	}
}
