Shader "Hidden/Vista/Splines/SplineMesh"
{
	CGINCLUDE
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"

	struct v2f
	{
		float4 vertex: SV_POSITION;
	};

	StructuredBuffer<float3> _TrianglesBuffer;

	v2f vert(uint id: SV_VERTEXID)
	{
		int index;
		int idMod6 = id % 6;
		int idBase = (id - idMod6) / 2;

		if (idMod6 == 0)
			index = idBase + 0;
		if (idMod6 == 1)
			index = idBase + 1;
		if (idMod6 == 2)
			index = idBase + 1;
		if (idMod6 == 3)
			index = idBase + 2;
		if (idMod6 == 4)
			index = idBase + 2;
		if (idMod6 == 5)
			index = idBase + 0;

		float3 worldPos = _TrianglesBuffer[index];
		float3 localPos = worldPos;

		v2f o;
		o.vertex = UnityObjectToClipPos(localPos);
		return o;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue"="Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			ZTest LEqual
			ZWrite Off
			Cull Off
			CGPROGRAM

			fixed4 frag(v2f i): SV_Target
			{
				return fixed4(0, 1, 1, 1);
			}
			ENDCG

		}
		Pass
		{
			ZTest Greater
			ZWrite Off
			Cull Off
			CGPROGRAM

			fixed4 frag(v2f i): SV_Target
			{
				return fixed4(0, 1, 1, 0.075);
			}
			ENDCG

		}
	}
}
