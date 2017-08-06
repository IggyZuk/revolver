// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "IMDraw/IMDraw Mesh Shader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		//BindChannels { Bind "Color",color }
		Blend SrcAlpha OneMinusSrcAlpha // Alpha blended
		//Blend SrcAlpha One // Additive blended
		ZWrite Off
		Cull Back
		Fog { Mode Off }

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			//#pragma fragmentoption ARB_precision_hint_fastest

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

			void frag (v2f i, out fixed4 colour:COLOR)
			{
				colour = _Color;
			}

			ENDCG	    
		} // Pass

	} // SubShader

} // Shader