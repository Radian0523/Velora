Shader "Velora/Dissolve_Lit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionMap ("Emission Map", 2D) = "white" {}

        [Header(UV Transform)]
        _UV_Offset ("UV Offset", Vector) = (0, 0, 0, 0)
        _UV_Tiling ("UV Tiling", Vector) = (1, 1, 0, 0)

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _NoiseScale ("Noise Scale", Float) = 4.0
        _EdgeWidth ("Edge Width", Float) = 0.05
        [HDR] _EdgeColor ("Edge Color", Color) = (3, 1.2, 0.2, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // =====================================================
        // Forward Pass: PBR Lighting + Dissolve Clip + Edge Emission
        // =====================================================
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                float4 _UV_Offset;
                float4 _UV_Tiling;
                half _DissolveAmount;
                float _NoiseScale;
                half _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                // _MAIN_LIGHT_SHADOWS_SCREEN 時はスクリーン座標からシャドウを取得するため、
                // ライトスペース座標は非スクリーン方式の場合のみ必要。
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
                #endif
            };

            // --------------------------------------------------
            // 3D Value Noise
            // オブジェクトスペース座標ベースにすることで、
            // 分離した 6 メッシュ間でノイズパターンが連続する。
            // --------------------------------------------------
            float Hash3D(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0, 0, 0));
                float n100 = Hash3D(i + float3(1, 0, 0));
                float n010 = Hash3D(i + float3(0, 1, 0));
                float n110 = Hash3D(i + float3(1, 1, 0));
                float n001 = Hash3D(i + float3(0, 0, 1));
                float n101 = Hash3D(i + float3(1, 0, 1));
                float n011 = Hash3D(i + float3(0, 1, 1));
                float n111 = Hash3D(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, f.x);
                float n10 = lerp(n010, n110, f.x);
                float n01 = lerp(n001, n101, f.x);
                float n11 = lerp(n011, n111, f.x);

                float n0 = lerp(n00, n10, f.y);
                float n1 = lerp(n01, n11, f.y);

                return lerp(n0, n1, f.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.texcoord * _UV_Tiling.xy + _UV_Offset.xy;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float noise = ValueNoise3D(input.positionWS * _NoiseScale);
                clip(noise - _DissolveAmount);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo = baseMap * _BaseColor;

                float3 normalWS = normalize(input.normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // 敵は動的オブジェクトのため lightmap は使用せず、
                // SH（Spherical Harmonics）からアンビエントライトを取得する。
                // SampleSHVertex → SampleSHPixel の 2 段階評価で全 SH バンドを合算する。
                half3 vertexSH = SampleSHVertex(normalWS);
                inputData.bakedGI = SampleSHPixel(vertexSH, normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                // 元マテリアルのエミッション（_EmissionMap × _EmissionColor）を再現する。
                half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                half3 baseEmission = emissionMap * _EmissionColor.rgb;

                // ディゾルブ境界でエッジ発光を加算。
                // DissolveAmount が 0 のときは step で完全に消し、通常描画への影響をゼロにする。
                half edgeFactor = 1.0 - saturate((noise - _DissolveAmount) / _EdgeWidth);
                half3 edgeEmission = _EdgeColor.rgb * edgeFactor * step(0.001, _DissolveAmount);
                surfaceData.emission = baseEmission + edgeEmission;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // =====================================================
        // Shadow Caster: dissolve clip only
        // =====================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                float4 _UV_Offset;
                float4 _UV_Tiling;
                half _DissolveAmount;
                float _NoiseScale;
                half _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float Hash3D(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0, 0, 0));
                float n100 = Hash3D(i + float3(1, 0, 0));
                float n010 = Hash3D(i + float3(0, 1, 0));
                float n110 = Hash3D(i + float3(1, 1, 0));
                float n001 = Hash3D(i + float3(0, 0, 1));
                float n101 = Hash3D(i + float3(1, 0, 1));
                float n011 = Hash3D(i + float3(0, 1, 1));
                float n111 = Hash3D(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, f.x);
                float n10 = lerp(n010, n110, f.x);
                float n01 = lerp(n001, n101, f.x);
                float n11 = lerp(n011, n111, f.x);

                float n0 = lerp(n00, n10, f.y);
                float n1 = lerp(n01, n11, f.y);

                return lerp(n0, n1, f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionWS = vertexInput.positionWS;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - vertexInput.positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(vertexInput.positionWS, normalInput.normalWS, lightDirectionWS));

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                float noise = ValueNoise3D(input.positionWS * _NoiseScale);
                clip(noise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }

        // =====================================================
        // DepthOnly: dissolve clip only
        // =====================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                float4 _UV_Offset;
                float4 _UV_Tiling;
                half _DissolveAmount;
                float _NoiseScale;
                half _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float Hash3D(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0, 0, 0));
                float n100 = Hash3D(i + float3(1, 0, 0));
                float n010 = Hash3D(i + float3(0, 1, 0));
                float n110 = Hash3D(i + float3(1, 1, 0));
                float n001 = Hash3D(i + float3(0, 0, 1));
                float n101 = Hash3D(i + float3(1, 0, 1));
                float n011 = Hash3D(i + float3(0, 1, 1));
                float n111 = Hash3D(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, f.x);
                float n10 = lerp(n010, n110, f.x);
                float n01 = lerp(n001, n101, f.x);
                float n11 = lerp(n011, n111, f.x);

                float n0 = lerp(n00, n10, f.y);
                float n1 = lerp(n01, n11, f.y);

                return lerp(n0, n1, f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                float noise = ValueNoise3D(input.positionWS * _NoiseScale);
                clip(noise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }

        // =====================================================
        // DepthNormals: dissolve clip only
        // =====================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                float4 _UV_Offset;
                float4 _UV_Tiling;
                half _DissolveAmount;
                float _NoiseScale;
                half _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float Hash3D(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0, 0, 0));
                float n100 = Hash3D(i + float3(1, 0, 0));
                float n010 = Hash3D(i + float3(0, 1, 0));
                float n110 = Hash3D(i + float3(1, 1, 0));
                float n001 = Hash3D(i + float3(0, 0, 1));
                float n101 = Hash3D(i + float3(1, 0, 1));
                float n011 = Hash3D(i + float3(0, 1, 1));
                float n111 = Hash3D(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, f.x);
                float n10 = lerp(n010, n110, f.x);
                float n01 = lerp(n001, n101, f.x);
                float n11 = lerp(n011, n111, f.x);

                float n0 = lerp(n00, n10, f.y);
                float n1 = lerp(n01, n11, f.y);

                return lerp(n0, n1, f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                float noise = ValueNoise3D(input.positionWS * _NoiseScale);
                clip(noise - _DissolveAmount);

                float3 normalWS = normalize(input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
