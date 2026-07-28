using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 슬롯머신의 전원 상태 — **쉼(rest) 조명의 유일한 주인.**
    ///
    /// 크레딧이 없으면 기계는 죽어 있다: 릴 창 조명이 꺼지고 발광 패널도 검게 식는다.
    /// 동전이 들어오면 '탁' 하고 살아난다. 어두운 방에서 기계가 유일하게 스스로 빛나는
    /// 물건이라, 이 소등/점등이 곧 "동전을 넣어라" 는 신호가 된다.
    ///
    /// 살아 있는 조건은 크레딧만이 아니다 — <see cref="SlotMachine.Pull"/> 이 크레딧을
    /// **먼저** 소모하므로 스핀 중에는 크레딧이 0 일 수 있고, 배출·펄스 중에도 마찬가지다.
    /// 그 순간들에 불이 꺼지면 기계가 고장난 것처럼 보인다. 그래서
    /// <see cref="Powered"/> 는 스핀·배출·펄스를 전부 살아 있는 상태로 친다.
    ///
    /// 발광 패널은 <c>renderer.material</c> 로 **런타임 인스턴스**를 떠서 만진다 —
    /// 공유 에셋(M_Emissive.mat)을 직접 바꾸면 에디터의 .mat 이 더러워지고 다음
    /// <c>NHNAI/Setup/1</c> 실행까지 값이 남는다. 인스턴스는 OnDestroy 에서 지운다.
    ///
    /// 릴 창 조명은 작성자가 둘이다 — 펄스 중에는 <see cref="SlotMachineWinEffect"/>,
    /// 쉴 때는 여기. 경계는 <see cref="SlotMachineWinEffect.Playing"/> 하나로 가른다.
    /// 쉼 세기의 정본은 이 컴포넌트의 <see cref="reelRestIntensity"/> 다 —
    /// 예전에 부트스트랩이 광원에 직접 박던 1.6 이 여기로 왔다.
    /// </summary>
    public sealed class SlotMachinePower : MonoBehaviour
    {
        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] SlotMachineView view;
        [Tooltip("릴 창 조명. 쉴 때의 세기를 이 컴포넌트가 쓴다")]
        [SerializeField] Light reelLight;
        [Tooltip("릴 뒤 발광 패널(M_Emissive). 소등은 런타임 인스턴스로 한다")]
        [SerializeField] Renderer backlightPanel;
        [SerializeField] CoinDispenser dispenser;
        [SerializeField] SlotMachineWinEffect winEffect;

        [Header("세기")]
        [Tooltip("릴 창 조명이 쉴 때의 세기. 펄스의 하한이기도 하다 — 올리면 연출도 같이 밝아진다")]
        [SerializeField] float reelRestIntensity = 1.6f;
        [Tooltip("켜지고 꺼지는 페이드(초). 짧아야 '탁' 하고 들어온다")]
        [SerializeField] float fadeTime = 0.15f;

        Material _panel;     // backlightPanel 의 런타임 인스턴스. 에셋이 아니다
        Color _emissionOn;   // 인스턴스를 뜬 직후의 원래 발광색 (white x 6, HDR)
        float _level;        // 페이드 진행값. 0 = 죽음, 1 = 살아 있음

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>기계가 살아 있는가. 크레딧 · 스핀 · 배출 · 펄스 중 하나라도 참이면 산다.</summary>
        public bool Powered => (view != null && (view.Credits > 0 || view.IsSpinning))
                            || (dispenser != null && dispenser.Dispensing)
                            || (winEffect != null && winEffect.Playing);

        /// <summary>릴 조명이 쉴 때의 세기. <see cref="SlotMachineWinEffect"/> 가
        /// 펄스의 하한으로 매번 읽는다 — 죽어 있으면 0 이라 펄스도 바닥부터 오른다.</summary>
        public float ReelRestIntensity => Powered ? reelRestIntensity : 0f;

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(SlotMachineView machineView, Light reel, Renderer panel,
                         CoinDispenser coinDispenser, SlotMachineWinEffect effect)
        {
            view = machineView;
            reelLight = reel;
            backlightPanel = panel;
            dispenser = coinDispenser;
            winEffect = effect;
        }

        void Awake()
        {
            if (backlightPanel == null) return;
            _panel = backlightPanel.material;   // 여기서 인스턴스가 떠진다
            _emissionOn = _panel.GetColor(EmissionColor);
            _panel.SetColor(EmissionColor, Color.black);   // 크레딧 0 으로 시작 — 첫 프레임부터 죽어 있다
        }

        void Update()
        {
            var target = Powered ? 1f : 0f;
            _level = Mathf.MoveTowards(_level, target,
                                       Time.deltaTime / Mathf.Max(fadeTime, 0.0001f));

            // 펄스 중에는 릴 조명을 건드리지 않는다 — 작성자는 하나여야 한다.
            if (reelLight != null && (winEffect == null || !winEffect.Playing))
                reelLight.intensity = reelRestIntensity * _level;

            if (_panel != null)
                _panel.SetColor(EmissionColor, Color.Lerp(Color.black, _emissionOn, _level));
        }

        void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);   // 인스턴스 누수 방지
        }
    }
}
