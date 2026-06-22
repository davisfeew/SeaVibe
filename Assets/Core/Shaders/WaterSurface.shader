Shader "SeaVibe/WaterSurface"
{
    Properties
    {
        _Color("Water Color", Color) = (0.1, 0.4, 0.7, 0.9)
    }
    SubShader
    {
        // Kreslíme vodu jako průhlednou, po vykreslení lodi a masky
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            // ZÁZRAK: Tady říkáme vodě, aby se nekreslila tam, kde je loď (Maska má Ref 1)
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            float4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Jednoduché nasvícení vln (Slunce)
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                
                // Difuzní světlo (odstíny vln)
                half NdotL = saturate(dot(normal, mainLight.direction));
                half3 diffuse = _Color.rgb * (NdotL * 0.6 + 0.4) * mainLight.color;
                
                // Odlesk slunce (Specular)
                float3 halfVector = normalize(mainLight.direction + input.viewDirWS);
                float NdotH = saturate(dot(normal, halfVector));
                float specular = pow(NdotH, 64.0) * 0.5;

                half3 finalColor = diffuse + (specular * mainLight.color);

                return half4(finalColor, _Color.a);
            }
            ENDHLSL
        }
    }
}
