// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "IMDraw/IMDraw GL Shader"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
	}

	CGINCLUDE
		
	#include "UnityCG.cginc"

	struct a2f
	{
		float4 vertex : POSITION;
		fixed4 colour : COLOR;
	};

	struct v2f
	{
		float4 pos : POSITION;
		fixed4 colour : COLOR;
	};

	v2f vert (a2f IN)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(IN.vertex);
		o.colour = IN.colour;

		return o;	
	}

	void frag (v2f i, out fixed4 colour : SV_Target)
	{
		colour = i.colour;
	}

	ENDCG	

	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			Lighting Off Cull Off ZTest [_ZTest] ZWrite Off
			Fog{ Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		} // Pass

	} // SubShader

} // Shader