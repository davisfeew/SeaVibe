Shader "SeaVibe/WaterMask"
{
    Properties { }
    SubShader
    {
        // Kreslíme po lodi, ale před vodou
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-100" }

        Pass
        {
            Name "StencilMask"
            Tags { "LightMode" = "UniversalForward" } // Explicitní URP pass
            
            Blend Zero One // Neviditelné míchání (neovlivní barvu za ním)
            ColorMask 0 // Zakáže zápis barvy
            ZWrite Off // Zakáže zápis do hloubky
            Cull Off // Vykreslí z obou stran pro jistotu
            ZTest LEqual

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
