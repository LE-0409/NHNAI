// 전등 아래 빛 기둥.
//
// URP 에는 볼류메트릭 라이팅이 없다(HDRP 전용). 그래서 원뿔 메시를 가산으로 그려
// 빛이 공기 중에 퍼지는 것처럼 보이게 한다.
//
// **고깔이 보이는 것이 의도다.** 사실적인 산란을 흉내내는 게 목표가 아니라,
// 어둠 속에 뚜렷한 원뿔이 서 있는 그림이 목표다.
//
// 그래서 실루엣을 지우지 않는다. 안개 속 빛기둥을 물리적으로 흉내내려면 시선이
// 통과하는 두께가 긴 가운데를 밝게(abs(dot(N,V))) 하는데, 그렇게 하면 가장자리가
// 녹아 원뿔 형태 자체가 사라진다. 여기서는 반대로 간다 —
//   · 몸통은 고르게 밝히고 (_Intensity)
//   · 테두리를 한 번 더 올려 윤곽을 세운다 (_RimBoost)
// 가산 블렌딩 + 양면이라 앞뒤 면이 겹쳐 더해지는 것도 형태를 또렷하게 만든다.
//
// 세로 방향은 전등에 가까운 위쪽이 밝고 바닥으로 갈수록 옅어지되,
// _BottomFade 아래로는 떨어지지 않는다 — 0 까지 떨어뜨리면 바닥 근처에서 고깔이 끊긴다.
//
// _TopY / _BottomY 는 **오브젝트 공간** Y 로, LightCone.fbx 메시의 위/아래 끝이다
// (Blender Z-up → Unity Y-up 변환 후 값). 메시를 다시 뽑으면 같이 맞춰야 한다 —
// generate_ceiling_lamp.py 가 실행 끝에 넣어야 할 값을 출력한다.
Shader "NHNAI/LightCone"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 4)) = 0.85
        _BottomY("Bottom Y (object space)", Float) = 0.02
        _TopY("Top Y (object space)", Float) = 4.48
        _Falloff("Vertical Falloff", Range(0.1, 6)) = 1.1
        _BottomFade("Bottom Fade Floor", Range(0, 1)) = 0.35
        _RimBoost("Rim Boost", Range(0, 4)) = 1.3
        _RimPower("Rim Power", Range(0.5, 8)) = 3.0
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
                float  _BottomFade;
                float  _RimBoost;
                float  _RimPower;
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

                // 스치는 각도 = 원뿔의 윤곽선. 여기를 올려 고깔의 테두리를 세운다.
                float rim = pow(saturate(1.0 - abs(dot(n, v))), _RimPower);
                // 전등 쪽이 밝고 바닥으로 갈수록 옅어지되 _BottomFade 아래로는 안 내려간다
                float fade = lerp(_BottomFade, 1.0, pow(IN.heightT, _Falloff));

                float a = fade * (1.0 + _RimBoost * rim) * _Intensity;
                return half4(_BaseColor.rgb * a, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
