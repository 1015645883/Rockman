Shader "Custom/RemoveColorShader"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _ColorKey("Key Color", Color) = (0.5, 0, 0.5, 1)
        _RemoveColor1("Remove Color 1", Color) = (0.58, 0.73, 0.92, 1)
        _Tolerance("Tolerance", Range(0, 1)) = 0.1

            // 6组替换颜色（所有RGB值已归一化到0-1范围）
            // 第一组（初始形态）
            _ReplaceFrom1("1-From Color A", Color) = (0, 0.439, 0.925, 1)    // RGB(0,112,236)
            _ReplaceTo1("1-To Color A", Color) = (0.455, 0.455, 0.455, 1)     // RGB(116,116,116)
            _ReplaceFrom2("1-From Color B", Color) = (0, 0.91, 0.847, 1)      // RGB(0,232,216)
            _ReplaceTo2("1-To Color B", Color) = (0.988, 0.988, 0.988, 1)     // RGB(252,252,252)

            // 第二组
            _ReplaceFrom3("2-From Color A", Color) = (0, 0.439, 0.925, 1)     // RGB(0,112,236)
            _ReplaceTo3("2-To Color A", Color) = (0.784, 0.298, 0.047, 1)     // RGB(200,76,12)
            _ReplaceFrom4("2-From Color B", Color) = (0, 0.91, 0.847, 1)      // RGB(0,232,216)
            _ReplaceTo4("2-To Color B", Color) = (0.988, 0.988, 0.988, 1)     // RGB(252,252,252)

            // 第三组
            _ReplaceFrom5("3-From Color A", Color) = (0, 0.439, 0.925, 1)     // RGB(0,112,236)
            _ReplaceTo5("3-To Color A", Color) = (0.125, 0.22, 0.925, 1)      // RGB(32,56,236)
            _ReplaceFrom6("3-From Color B", Color) = (0, 0.91, 0.847, 1)      // RGB(0,232,216)
            _ReplaceTo6("3-To Color B", Color) = (0.988, 0.988, 0.988, 1)     // RGB(252,252,252)

            // 第四组
            _ReplaceFrom7("4-From Color A", Color) = (0, 0.439, 0.925, 1)     // RGB(0,112,236)
            _ReplaceTo7("4-To Color A", Color) = (0, 0.58, 0, 1)              // RGB(0,148,0)
            _ReplaceFrom8("4-From Color B", Color) = (0, 0.91, 0.847, 1)      // RGB(0,232,216)
            _ReplaceTo8("4-To Color B", Color) = (0.988, 0.988, 0.988, 1)     // RGB(252,252,252)

            // 第五组
            _ReplaceFrom9("5-From Color A", Color) = (0, 0.439, 0.925, 1)     // RGB(0,112,236)
            _ReplaceTo9("5-To Color A", Color) = (0.847, 0.157, 0, 1)         // RGB(216,40,0)
            _ReplaceFrom10("5-From Color B", Color) = (0, 0.91, 0.847, 1)     // RGB(0,232,216)
            _ReplaceTo10("5-To Color B", Color) = (0.94, 0.737, 0.235, 1)     // RGB(240,188,60)

            // 第六组
            _ReplaceFrom11("6-From Color A", Color) = (0, 0.439, 0.925, 1)    // RGB(0,112,236)
            _ReplaceTo11("6-To Color A", Color) = (0.455, 0.455, 0.455, 1)    // RGB(116,116,116)
            _ReplaceFrom12("6-From Color B", Color) = (0, 0.91, 0.847, 1)     // RGB(0,232,216)
            _ReplaceTo12("6-To Color B", Color) = (0.988, 0.894, 0.627, 1)    // RGB(252,228,160)


            // 形态选择(0=无替换，1-6对应6种形态)
            _ColorReplaceMode("Color Replace Mode", Range(0, 6)) = 0
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            LOD 100
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                fixed4 _ColorKey;
                fixed4 _RemoveColor1;
                float _Tolerance;

                // 12组替换颜色（6种形态×2组颜色）
                fixed4 _ReplaceFrom1; fixed4 _ReplaceTo1;
                fixed4 _ReplaceFrom2; fixed4 _ReplaceTo2;
                fixed4 _ReplaceFrom3; fixed4 _ReplaceTo3;
                fixed4 _ReplaceFrom4; fixed4 _ReplaceTo4;
                fixed4 _ReplaceFrom5; fixed4 _ReplaceTo5;
                fixed4 _ReplaceFrom6; fixed4 _ReplaceTo6;
                fixed4 _ReplaceFrom7; fixed4 _ReplaceTo7;
                fixed4 _ReplaceFrom8; fixed4 _ReplaceTo8;
                fixed4 _ReplaceFrom9; fixed4 _ReplaceTo9;
                fixed4 _ReplaceFrom10; fixed4 _ReplaceTo10;
                fixed4 _ReplaceFrom11; fixed4 _ReplaceTo11;
                fixed4 _ReplaceFrom12; fixed4 _ReplaceTo12;

                int _ColorReplaceMode;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);

                // 透明处理
                float dist1 = distance(col.rgb, _ColorKey.rgb);
                float dist2 = distance(col.rgb, _RemoveColor1.rgb);
                if (dist1 < _Tolerance || dist2 < _Tolerance)
                    col.a = 0;

                // 颜色替换逻辑
                if (_ColorReplaceMode > 0)
                {
                    // 第一形态
                    if (_ColorReplaceMode == 1)
                    {
                        if (distance(col.rgb, _ReplaceFrom1.rgb) < _Tolerance) col.rgb = _ReplaceTo1.rgb;
                        if (distance(col.rgb, _ReplaceFrom2.rgb) < _Tolerance) col.rgb = _ReplaceTo2.rgb;
                    }
                    // 第二形态
                    else if (_ColorReplaceMode == 2)
                    {
                        if (distance(col.rgb, _ReplaceFrom3.rgb) < _Tolerance) col.rgb = _ReplaceTo3.rgb;
                        if (distance(col.rgb, _ReplaceFrom4.rgb) < _Tolerance) col.rgb = _ReplaceTo4.rgb;
                    }
                    // 第三形态
                    else if (_ColorReplaceMode == 3)
                    {
                        if (distance(col.rgb, _ReplaceFrom5.rgb) < _Tolerance) col.rgb = _ReplaceTo5.rgb;
                        if (distance(col.rgb, _ReplaceFrom6.rgb) < _Tolerance) col.rgb = _ReplaceTo6.rgb;
                    }
                    // 第四形态
                    else if (_ColorReplaceMode == 4)
                    {
                        if (distance(col.rgb, _ReplaceFrom7.rgb) < _Tolerance) col.rgb = _ReplaceTo7.rgb;
                        if (distance(col.rgb, _ReplaceFrom8.rgb) < _Tolerance) col.rgb = _ReplaceTo8.rgb;
                    }
                    // 第五形态
                    else if (_ColorReplaceMode == 5)
                    {
                        if (distance(col.rgb, _ReplaceFrom9.rgb) < _Tolerance) col.rgb = _ReplaceTo9.rgb;
                        if (distance(col.rgb, _ReplaceFrom10.rgb) < _Tolerance) col.rgb = _ReplaceTo10.rgb;
                    }
                    // 第六形态
                    else if (_ColorReplaceMode == 6)
                    {
                        if (distance(col.rgb, _ReplaceFrom11.rgb) < _Tolerance) col.rgb = _ReplaceTo11.rgb;
                        if (distance(col.rgb, _ReplaceFrom12.rgb) < _Tolerance) col.rgb = _ReplaceTo12.rgb;
                    }
                }

                return col;
            }
            ENDCG
        }
        }
}