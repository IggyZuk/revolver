Shader "Custom/CellShade" {
    Properties
    {
        _Color("Multiplicative Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AdditiveColor("Additive Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _RimColor("Rim Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        CGPROGRAM

        #pragma surface surf CelShadingForward #pragma target 3.0

        half4 LightingCelShadingForward(SurfaceOutput s, half3 lightDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            if (NdotL <= 0.0) NdotL = 0;
            else NdotL = smoothstep(0.3, 0.5, NdotL);

            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * ((NdotL * atten) + 0.5);
            c.a = s.Alpha;
            return c;
        }

        sampler2D _MainTex;
        fixed4 _Color;
        float4 _RimColor;
        float _RimPower;
        float4 _AdditiveColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            float3 final = (c.rgb - 0.25) + (_RimColor.rgb * pow(rim, _RimPower));
            final += _AdditiveColor.rgb;
            o.Albedo = final;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
