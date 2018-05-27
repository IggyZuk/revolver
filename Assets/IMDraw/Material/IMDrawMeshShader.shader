// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "IMDraw/IMDraw Mesh Shader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	fixed4 _Color;

	struct a2f
	{
		float4 vertex : POSITION;
	};

	struct v2f
	{
		float4 pos : POSITION;
	};

	v2f vert (a2f IN)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(IN.vertex);

		return o;	
	}

	void frag (v2f i, out fixed4 colour : SV_Target)
	{
		colour = _Color;
	}

	ENDCG	

	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			Lighting Off Cull Back ZTest [_ZTest] ZWrite Off
			Fog{ Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		} // Pass

	} // SubShader

} // Shader