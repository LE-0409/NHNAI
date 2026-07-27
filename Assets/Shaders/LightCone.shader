// 전등 아래 빛 기둥.
//
// URP 에는 볼류메트릭 라이팅이 없다(HDRP 전용). 그래서 원뿔 메시를 가산으로 그려
// 빛이 공기 중에 퍼지는 것처럼 보이게 한다.
//
// **고깔이 보이는 것이 의도다.** 사실적인 산란을 흉내내는 게 목표가 아니라,
// 어둠 속에 뚜렷한 원뿔이 서 있는 그림이 목표다. 그래서 실루엣을 통째로 녹이지 않는다.
// 다만 '보인다' 와 '칼로 자른 것 같다' 는 다르므로 경계를 세 개의 손잡이로 나눠 두었다.
//
//   _DepthFade     원뿔이 바닥·벽을 관통하며 생기는 교차선을 지운다.
//                  손대지 않았을 때 가장 어색해 보이는 지점이 여기다.
//                  0 이면 기능을 끈다 (깊이 텍스처가 없는 환경의 안전장치).
//   _EdgeSoftness  실루엣이 사라지는 폭. 0 에 가까우면 칼단면, 크면 뭉근하게 사라진다.
//                  몸통 밝기는 건드리지 않아서 고깔 형태는 남는다.
//   _RimBoost      테두리를 한 번 더 밝혀 윤곽을 세운다. 경계가 과하면 여기부터 낮춘다.
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

        [Header(Shape)]
        _BottomY("Bottom Y (object space)", Float) = 0.02
        _TopY("Top Y (object space)", Float) = 4.48
        _Falloff("Vertical Falloff", Range(0.1, 6)) = 1.1
        _BottomFade("Bottom Fade Floor", Range(0, 1)) = 0.35

        [Header(Edges)]
        _DepthFade("Depth Fade (m, 0 = off)", Range(0, 4)) = 1.2
        _EdgeSoftness("Silhouette Softness", Range(0, 1)) = 0.35
        _RimBoost("Rim Boost", Range(0, 4)) = 0.6
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float  heightT    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Intensity;
                float  _BottomY;
                float  _TopY;
                float  _Falloff;
                float  _BottomFade;
                float  _DepthFade;
                float  _EdgeSoftness;
                float  _RimBoost;
                float  _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.heightT    = saturate((IN.positionOS.y - _BottomY) / max(_TopY - _BottomY, 1e-4));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float facing = abs(dot(n, v));   // 1 = 정면, 0 = 실루엣

                // 전등 쪽이 밝고 바닥으로 갈수록 옅어지되 _BottomFade 아래로는 안 내려간다
                float fade = lerp(_BottomFade, 1.0, pow(IN.heightT, _Falloff));

                // 실루엣이 사라지는 폭. 몸통(facing 이 큰 영역)은 그대로 두고 끝만 녹인다.
                // _EdgeSoftness = 0 이면 smoothstep 구간이 없어져 예전처럼 칼단면이 된다.
                float edge = _EdgeSoftness > 1e-4
                    ? smoothstep(0.0, _EdgeSoftness, facing)
                    : 1.0;

                // 테두리 강조. 경계가 과하게 보이면 이 값부터 낮춘다.
                float rim = pow(saturate(1.0 - facing), _RimPower);

                // 바닥·벽과 만나는 교차선 제거. 뒤에 있는 불투명 물체와의 거리가
                // 가까울수록 흐려진다 — 소프트 파티클과 같은 원리다.
                float depthFade = 1.0;
                if (_DepthFade > 1e-4)
                {
                    float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                    float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    float fragEye = -TransformWorldToView(IN.positionWS).z;
                    depthFade = saturate((sceneEye - fragEye) / _DepthFade);
                }

                float a = fade * edge * depthFade * (1.0 + _RimBoost * rim) * _Intensity;
                return half4(_BaseColor.rgb * a, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
