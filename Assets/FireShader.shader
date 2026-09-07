Shader "Custom/URP-2D-AdvancedCharacterLit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        // === Remove-color controls ===
        _ColorKey("Key Color", Color) = (0.5, 0, 0.5, 1)
        _RemoveColor1("Remove Color 1", Color) = (0.58, 0.73, 0.92, 1)
        _RemoveColor2("Remove Color 2", Color) = (0.737, 0, 0.737, 1)
        _RemoveColor3("Remove Color 3", Color) = (0.3294, 0.6471, 0.2941, 1)
        _Tolerance("Tolerance", Range(0, 1)) = 0.1

            // === 蓄力相关 ===
            _ChargeLevel("Charge Level", Range(0, 2)) = 0
            _CycleSpeed("Cycle Speed", Float) = 5.0

            // === Unity legacy support ===
            [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
            [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
            [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
            [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
            [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

        SubShader
            {
                Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
                Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
                Cull Off
                ZWrite Off

                Pass
                {
                    Tags { "LightMode" = "Universal2D" }

                    HLSLPROGRAM
                    #pragma vertex CombinedShapeLightVertex
                    #pragma fragment CombinedShapeLightFragment

                    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

                    #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
                    #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
                    #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
                    #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
                    #pragma multi_compile _ DEBUG_DISPLAY

                    struct Attributes
                    {
                        float3 positionOS : POSITION;
                        float4 color      : COLOR;
                        float2 uv         : TEXCOORD0;
                        UNITY_VERTEX_INPUT_INSTANCE_ID
                    };

                    struct Varyings
                    {
                        float4 positionCS : SV_POSITION;
                        half4 color       : COLOR;
                        float2 uv         : TEXCOORD0;
                        half2 lightingUV  : TEXCOORD1;
                        UNITY_VERTEX_OUTPUT_STEREO
                    };

                    TEXTURE2D(_MainTex);
                    SAMPLER(sampler_MainTex);
                    TEXTURE2D(_MaskTex);
                    SAMPLER(sampler_MaskTex);
                    float4 _Color;
                    half4 _RendererColor;

                    float4 _ColorKey;
                    float4 _RemoveColor1;
                    float4 _RemoveColor2;
                    float4 _RemoveColor3;
                    float _Tolerance;

                    float _ChargeLevel;
                    float _CycleSpeed;

                    #if USE_SHAPE_LIGHT_TYPE_0
                    SHAPE_LIGHT(0)
                    #endif
                    #if USE_SHAPE_LIGHT_TYPE_1
                    SHAPE_LIGHT(1)
                    #endif
                    #if USE_SHAPE_LIGHT_TYPE_2
                    SHAPE_LIGHT(2)
                    #endif
                    #if USE_SHAPE_LIGHT_TYPE_3
                    SHAPE_LIGHT(3)
                    #endif

                    Varyings CombinedShapeLightVertex(Attributes v)
                    {
                        Varyings o = (Varyings)0;
                        UNITY_SETUP_INSTANCE_ID(v);
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        o.positionCS = TransformObjectToHClip(v.positionOS);
                        o.uv = v.uv;
                        o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                        o.color = v.color * _Color * _RendererColor;
                        return o;
                    }

                    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

                    // === 蓄力动态颜色函数 ===
                    float3 GetCycledColor(float3 originalColor, float time)
                    {
                        if (_ChargeLevel < 0.5)
                            return originalColor;

                        // 二阶蓄力（闪烁）
                        if (_ChargeLevel >= 1.5)
                        {
                            if (originalColor.r < 0.01 && originalColor.g < 0.01 && originalColor.b < 0.01)
                            {
                                float3 c1 = float3(0.66, 0.0, 0.0);
                                float3 c2 = float3(0.89, 0.0, 0.34);
                                float3 c3 = float3(0.99, 0.45, 0.70);
                                float cycle = fmod(time * _CycleSpeed, 3.0);
                                if (cycle < 1.0) return lerp(c1, c2, cycle);
                                if (cycle < 2.0) return lerp(c2, c3, cycle - 1.0);
                                return lerp(c3, c1, cycle - 2.0);
                            }
                        }
                        else // 一阶蓄力
                        {
                            if (originalColor.r < 0.01 && originalColor.g < 0.01 && originalColor.b < 0.01)
                            {
                                float3 c1 = float3(0.0, 0.91, 0.85);
                                float3 c2 = float3(0.0, 0.44, 0.93);
                                float3 c3 = float3(0.0, 0.0, 0.0);
                                float cycle = fmod(time * _CycleSpeed, 3.0);
                                if (cycle < 1.0) return lerp(c1, c2, cycle);
                                if (cycle < 2.0) return lerp(c2, c3, cycle - 1.0);
                                return lerp(c3, c1, cycle - 2.0);
                            }
                        }
                        return originalColor;
                    }

                    half4 CombinedShapeLightFragment(Varyings i) : SV_Target
                    {
                        half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                        // === 颜色移除判断 ===
                        float3 diff1 = texCol.rgb - _ColorKey.rgb;
                        float3 diff2 = texCol.rgb - _RemoveColor1.rgb;
                        float3 diff3 = texCol.rgb - _RemoveColor2.rgb;
                        float3 diff4 = texCol.rgb - _RemoveColor3.rgb;
                        float tol = max(_Tolerance, 0.000001);
                        float tolSq = tol * tol;
                        if (dot(diff1, diff1) < tolSq || dot(diff2, diff2) < tolSq || dot(diff3, diff3) < tolSq || dot(diff4, diff4) < tolSq)
                            discard;

                        // === 蓄力动态变色 ===
                        texCol.rgb = GetCycledColor(texCol.rgb, _Time.y);

                        const half4 main = i.color * texCol;
                        const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);

                        SurfaceData2D surfaceData;
                        InputData2D inputData;
                        InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                        InitializeInputData(i.uv, i.lightingUV, inputData);

                        return CombinedShapeLightShared(surfaceData, inputData);
                    }
                    ENDHLSL
                }
            }

                Fallback "Sprites/Default"
}
