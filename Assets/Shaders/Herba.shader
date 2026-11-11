Shader "TFG/Herba"
{
    // Herba de les illes. El color ve de cada vèrtex (el posa GrassField), es mou amb el vent,
    // s'aparta quan hi passa l'aranya i, de lluny, s'encongeix fins a desaparèixer (sense salts)
    Properties
    {
        _WindStrength ("Força del vent", Range(0, 1)) = 0.25
        _WindSpeed ("Velocitat del vent", Range(0, 5)) = 1.3
        _WindScale ("Mida de les ràfegues", Range(0.01, 1)) = 0.1
        _WindDirection ("Direcció del vent (X, Z)", Vector) = (1, 0, 0.35, 0)
        _PushStrength ("Quant s'aparta per l'aranya", Range(0, 2)) = 1.1
        _Translucency ("Llum a contrallum a les puntes", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _WindStrength;
            float _WindSpeed;
            float _WindScale;
            float4 _WindDirection;
            float _PushStrength;
            float _Translucency;
        CBUFFER_END

        // Valors globals que posa el component GrassField
        float4 _HerbaPusher; // xyz: on és l'aranya; w: radi (0 = no aparta res)
        float4 _HerbaFade;   // x: distància on comença a amagar-se; y: on ja no es veu (metres des de la càmera)

        // uv.x = alçada de la fulla (m); uv.y = 0 a la base i 1 a la punta
        float3 BendGrass(float3 positionWS, float2 uv)
        {
            float height = uv.x;
            float tip = uv.y;
            float bend = tip * tip;
            float3 root = positionWS - float3(0.0, tip * height, 0.0);

            // Vent: dues ones que es sumen, perquè no es mogui tot alhora
            float phase = _Time.y * _WindSpeed + dot(root.xz, float2(1.0, 0.7)) * _WindScale;
            float gust = sin(phase) * 0.65 + sin(phase * 2.17 + root.x * 0.9) * 0.35;
            float2 windDirection = normalize(_WindDirection.xz + float2(0.0001, 0.0));
            float3 offset = float3(windDirection.x, 0.0, windDirection.y) * (gust * 0.5 + 0.5) * _WindStrength * bend * height;

            // L'aranya aparta l'herba quan hi passa
            if (_HerbaPusher.w > 0.0)
            {
                float2 away = root.xz - _HerbaPusher.xz;
                float distanceXZ = length(away);
                float push = saturate(1.0 - distanceXZ / _HerbaPusher.w) * saturate(1.0 - abs(root.y - _HerbaPusher.y) / 2.5);
                offset.xz += away / max(distanceXZ, 0.001) * push * _PushStrength * bend * height;
                offset.y -= push * 0.5 * tip * height;
            }

            // De lluny, la fulla s'encongeix cap a terra
            float fade = 1.0;
            if (_HerbaFade.y > 0.0)
            {
                fade = saturate((_HerbaFade.y - distance(root, GetCameraPositionWS())) / max(_HerbaFade.y - _HerbaFade.x, 0.01));
            }
            return lerp(root, positionWS + offset, fade);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                float fogFactor : TEXCOORD2;
                float tip : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = BendGrass(TransformObjectToWorld(input.positionOS.xyz), input.uv);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.tip = input.uv.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Les fulles fan servir la normal del terra (s'il·luminen igual que el terra) i, a contrallum, les puntes brillen una mica
                half direct = saturate(dot(normalWS, mainLight.direction));
                float3 viewDirection = normalize(GetCameraPositionWS() - input.positionWS);
                half backLight = saturate(dot(-viewDirection, mainLight.direction)) * _Translucency * input.tip;

                half3 albedo = input.color.rgb;
                half3 color = albedo * mainLight.color * (direct + backLight) * shadow + albedo * SampleSH(normalWS);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Profunditat (fa falta perquè l'herba surti a la imatge de profunditat que fan servir alguns efectes)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings VertDepth(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(BendGrass(TransformObjectToWorld(input.positionOS.xyz), input.uv));
                return output;
            }

            half FragDepth(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // Profunditat i normals (les fa servir l'oclusió ambiental, SSAO)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex VertNormals
            #pragma fragment FragNormals
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings VertNormals(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(BendGrass(TransformObjectToWorld(input.positionOS.xyz), input.uv));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 FragNormals(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
