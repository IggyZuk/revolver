// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "IMDraw/IMDraw GL Shader"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		//BindChannels { Bind "Color",color }
		Blend SrcAlpha OneMinusSrcAlpha // Alpha blended
		//Blend SrcAlpha One // Additive blended
		ZWrite Off
		Cull Off
		Fog { Mode Off }

		Pass
		{
			CGPROGRAM
		
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			//#pragma fragmentoption ARB_precision_hint_fastest

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

			void frag (v2f i, out fixed4 colour:COLOR)
			{
				colour = i.colour;
			}

			ENDCG	    
		} // Pass

	} // SubShader

} // Shader