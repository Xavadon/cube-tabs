Shader "Skybox/Gradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.3, 0.52, 0.85, 1)
        _HorizonColor ("Horizon Color", Color) = (0.78, 0.84, 0.88, 1)
        _HorizonExponent ("Horizon Exponent", Range(1, 10)) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            half _HorizonExponent;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half up = saturate(normalize(i.dir).y);
                half t = 1.0 - pow(1.0 - up, _HorizonExponent);
                half3 color = lerp(_HorizonColor.rgb, _TopColor.rgb, t);

                half noise = frac(sin(dot(i.pos.xy, float2(12.9898, 78.233))) * 43758.5453);
                color += (noise - 0.5) / 255.0;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
