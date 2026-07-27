// 전등 아래 빛 기둥.
//
// URP 에는 볼류메트릭 라이팅이 없다(HDRP 전용). 그래서 원뿔 메시를 가산으로 그려
// 빛이 공기 중에 퍼지는 것처럼 보이게 한다.
//
// 핵심은 가장자리 처리다. 그냥 반투명하게 칠하면 딱딱한 원뿔 실루엣이 보여 '빛'이
// 아니라 '물체'로 읽힌다. 안개 속 빛기둥은 시선이 통과하는 두께가 가운데에서
// 가장 길어 가운데가 밝고 가장자리로 갈수록 사라진다. 그래서 법선이 카메라를
// 마주보는 곳(가운데)을 밝게, 스치는 곳(실루엣)을 어둡게 한다 — abs(dot(N,V)).
// 프레넬을 반대로 쓰는 흔한 실수를 하면 테두리만 빛나는 고깔이 된다.
//
// 세로 방향은 전등에 가까운 위쪽이 밝고 바닥으로 갈수록 옅어진다.
// _TopY / _BottomY 는 **오브젝트 공간** Y 로, LightCone.fbx 메시의 위/아래 끝이다
// (Blender Z-up → Unity Y-up 변환 후 값). 메시를 다시 뽑으면 같이 맞춰야 한다.
Shader "NHNAI/LightCone"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 3)) = 0.30
        _BottomY("Bottom Y (object space)", Float) = 0.02
        _TopY("Top Y (object space)", Float) = 2.28
        _Falloff("Vertical Falloff", Range(0.1, 6)) = 1.8
        _EdgeSoftness("Edge Softness", Range(0.1, 6)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One      // 가산 — 빛은 더해질 뿐 뒤를 가리지 않는다
        ZWrite Off         // 깊이를 쓰면 뒤의 물체가 잘려나간다
        Cull Off           // 원뿔 안에 들어가도 보여야 한다
        ZTest LEqual

        Pass
        {
            Name "LightCone"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float  heightT    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Intensity;
                float  _BottomY;
                float  _TopY;
                float  _Falloff;
                float  _EdgeSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                OUT.heightT    = saturate((IN.positionOS.y - _BottomY) / max(_TopY - _BottomY, 1e-4));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);

                // 가운데(법선이 카메라를 마주봄) 밝게, 실루엣(스침) 어둡게
                float soft = pow(saturate(abs(dot(n, v))), _EdgeSoftness);
                // 전등 쪽이 밝고 바닥으로 갈수록 옅어짐
                float fade = pow(IN.heightT, _Falloff);

                float a = soft * fade * _Intensity;
                return half4(_BaseColor.rgb * a, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
