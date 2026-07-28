using System.Collections.Generic;
using NHNAI.Game.Coins;
using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 당첨 동전을 뱉는 배출기 — **배출 타임라인의 주인.** 기계 루트에 붙는다.
    ///
    /// <see cref="SlotMachineView"/> 가 개수를 <see cref="Dispense"/> · <see cref="Refund"/>
    /// 로 쌓아 두면, 여기서 일정 간격으로 하나씩 배출구(PayoutMouth) 앞에 스폰해
    /// 트레이로 떨어뜨린다. 배출음은 같은 자리에서 PlayOneShot 으로 겹쳐 낸다
    /// (소리 수 = 동전 수). 큰 성공 100개 x 0.1초 = 10초짜리 소나기다.
    ///
    /// **출구는 하나지만 돈의 성격은 둘이다.** 딴 돈(<see cref="Dispense"/>)은 스폰마다
    /// <see cref="SlotMachineWinEffect.FlashCoin"/> 으로 펄스를 하나 달고 나오고,
    /// 되찾는 돈(<see cref="Refund"/>)은 빛 없이 조용히 나온다 — 넣어 둔 것을 도로
    /// 꺼내는 일까지 번쩍이면 번쩍임이 "땄다" 는 뜻을 잃는다.
    /// 드라이버가 하나라 "번쩍임 수 = 당첨 동전 수" 가 어긋날 길이 없다.
    ///
    /// 트레이 조명도 여기서 관리한다: 배출 중이거나 배출된 동전이 트레이 부근에
    /// 남아 있는 동안 켜지고, 다 주워 가면 서서히 꺼진다. 전원 상태와 **독립**이다 —
    /// 크레딧이 떨어졌다고 트레이의 동전까지 캄캄해지면 딴 돈이 안 보인다.
    /// </summary>
    public sealed class CoinDispenser : MonoBehaviour
    {
        [Header("배출")]
        [Tooltip("동전 사이 간격(초). WinEffect.pulseTime 과 같아야 펄스가 끊김 없이 이어진다")]
        [SerializeField] float interval = 0.1f;
        [Tooltip("배출구에서 나오는 전방 속도(m/s). generate_slot_machine.py 의 낙하 검산과 같은 값")]
        [SerializeField] float ejectSpeed = 0.3f;

        [Header("사운드")]
        [Tooltip("동전 한 개가 나올 때마다 재생. PlayOneShot 이라 앞 소리를 끊지 않고 겹친다")]
        [SerializeField] AudioClip coinClip;
        [Tooltip("배출음 음량")]
        [SerializeField, Range(0f, 1f)] float coinVolume = 0.9f;

        [Header("트레이 조명")]
        [Tooltip("트레이 조명이 켜졌을 때의 세기")]
        [SerializeField] float trayLightIntensity = 2.2f;
        [Tooltip("트레이 앵커 기준 이 반경(m) 안에 동전이 있으면 조명을 켜 둔다")]
        [SerializeField] float trayRadius = 0.45f;
        [Tooltip("조명이 켜지고 꺼지는 페이드(초)")]
        [SerializeField] float lightFade = 0.3f;

        [Header("부품 — 부트스트랩이 채운다")]
        [Tooltip("씬의 비활성 CoinTemplate. 복제해서 뱉는다")]
        [SerializeField] GameObject coinTemplate;
        [Tooltip("FBX 의 PayoutMouth 트랜스폼. 원점이 개구 앞이다")]
        [SerializeField] Transform mouth;
        [Tooltip("FBX 의 CoinTray 트랜스폼. 원점이 안쪽 바닥 중앙이다")]
        [SerializeField] Transform tray;
        [SerializeField] Light trayLight;
        [SerializeField] SlotMachineWinEffect winEffect;

        readonly List<Coin> _spawned = new();

        int _pending;       // 아직 못 뱉은 동전 수. 배출 중 재당첨이면 그냥 쌓인다
        int _quiet;         // 그중 빛 없이 나갈 몫(환불). _pending 의 부분집합이다
        float _timer;
        float _lightLevel;
        AudioSource _audio;

        /// <summary>배출이 진행 중인가. 전원 상태가 '살아 있음' 의 근거로 읽는다.</summary>
        public bool Dispensing => _pending > 0;

        /// <summary>딴 돈을 쌓는다 — 동전마다 빛 펄스가 하나 따라 나온다.
        /// 배출은 Update 가 간격을 지켜 가며 한다.</summary>
        public void Dispense(int count)
        {
            if (count > 0) _pending += count;
        }

        /// <summary>
        /// 되찾는 돈을 쌓는다 — 같은 출구, 같은 소리, 다만 **빛은 없다.**
        ///
        /// 개수를 따로 세는 이유: 배출 타임라인이 하나뿐이라 "지금 배출 중인 것이
        /// 어느 쪽인가" 를 bool 로 잡으면 두 종류가 섞이는 순간 앞의 몫까지
        /// 뒤엎는다. 지금은 <see cref="SlotMachineView.CanRefund"/> 가 배출·징글 중을
        /// 막아 섞일 길이 없지만, 게이트가 느슨해져도 개수는 거짓말을 하지 않는다.
        /// </summary>
        public void Refund(int count)
        {
            if (count <= 0) return;
            _pending += count;
            _quiet += count;
        }

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(GameObject template, Transform mouthAnchor, Transform trayAnchor,
                         Light light, SlotMachineWinEffect effect, AudioClip clip)
        {
            coinTemplate = template;
            mouth = mouthAnchor;
            tray = trayAnchor;
            trayLight = light;
            winEffect = effect;
            coinClip = clip;
        }

        void Awake()
        {
            // 전역 물리 튜닝의 적용 지점. 기본 2 m/s 는 그보다 느린 충돌의 반발을 전부
            // 죽이는데, 트레이 안에서 동전끼리 부딪히는 속도는 대부분 그 아래다.
            // 값의 정본은 Coin.BounceThreshold — 여기는 적용만 한다.
            Physics.bounceThreshold = Coin.BounceThreshold;

            // 배출음 스피커. 배출구에 붙여야 소리가 트레이 쪽에서 난다.
            // PlayOneShot 전용이라 소스 하나로도 소리가 겹쳐 쌓인다 — 끊고 다시 틀지 않는다.
            var speaker = mouth != null ? mouth.gameObject : gameObject;
            _audio = speaker.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;   // 3D — 기계 위치에서 들려온다
            // 도플러를 끈다. 켜 두면 리스너(카메라)가 기계 반대쪽을 볼 때 소리가
            // 끊기는 Unity 버그가 있다 (Unity 6 에서도 재현). 배출구는 정지물이다.
            _audio.dopplerLevel = 0f;
        }

        void Update()
        {
            TickSpawn(Time.deltaTime);
            TickLight(Time.deltaTime);
        }

        void TickSpawn(float deltaTime)
        {
            if (_pending <= 0) return;

            _timer -= deltaTime;
            if (_timer > 0f) return;
            _timer = interval;
            _pending--;

            // 조용한 몫을 먼저 흘려보낸다. 섞이지 않는 것이 전제라 순서가 결과를
            // 바꾸지 않지만, 정해 두지 않으면 섞였을 때 프레임마다 달라진다.
            var quiet = _quiet > 0;
            if (quiet) _quiet--;

            Spawn();
            if (!quiet && winEffect != null) winEffect.FlashCoin();   // 당첨 스폰 1 = 펄스 1
        }

        void Spawn()
        {
            if (coinTemplate == null || mouth == null) return;

            // 개구 앞 5 cm — 캐비닛 콜라이더 바깥이라 태어나자마자 밀려나지 않는다.
            var pos = mouth.position + transform.forward * 0.05f;
            // 가로 슬릿에서 눕다시피 나온다 (기본 자세 = 면이 위). 자세와 회전을 조금씩
            // 흩어야 같은 자리에 정확히 포개져 물리가 폭발하듯 밀어내는 것을 막는다.
            var rot = Quaternion.Euler(Random.Range(-12f, 12f),
                                       Random.Range(0f, 360f),
                                       Random.Range(-12f, 12f));

            // 템플릿과 같은 부모(Coins) 밑에 — 씬 루트가 동전 백 개로 어질러지지 않게.
            var go = Instantiate(coinTemplate, pos, rot, coinTemplate.transform.parent);
            go.SetActive(true);

            if (go.TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = transform.forward * ejectSpeed + Vector3.down * 0.1f;
                body.angularVelocity = new Vector3(Random.Range(-6f, 6f),
                                                   Random.Range(-6f, 6f),
                                                   Random.Range(-6f, 6f));
            }

            if (go.TryGetComponent<Coin>(out var coin)) _spawned.Add(coin);

            // 스폰 1 = 사운드 1. 펄스와 같은 원칙이라 "소리 수 = 동전 수" 가 어긋날 길이 없다.
            if (coinClip != null) _audio.PlayOneShot(coinClip, coinVolume);
        }

        void TickLight(float deltaTime)
        {
            if (trayLight == null) return;

            var want = Dispensing || AnyCoinNearTray() ? 1f : 0f;
            _lightLevel = Mathf.MoveTowards(_lightLevel, want,
                                            deltaTime / Mathf.Max(lightFade, 0.0001f));
            trayLight.intensity = trayLightIntensity * _lightLevel;
        }

        // 매 프레임 O(n) 이지만 거리 제곱 비교뿐이라 동전 수백 개도 문제없다.
        // 주워서(파괴돼서) 빈 자리는 지나는 길에 정리한다.
        bool AnyCoinNearTray()
        {
            if (tray == null) return false;

            var r2 = trayRadius * trayRadius;
            var near = false;
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                var coin = _spawned[i];
                if (coin == null)
                {
                    _spawned.RemoveAt(i);
                    continue;
                }
                if (!coin.IsHeld
                    && (coin.transform.position - tray.position).sqrMagnitude < r2)
                    near = true;
            }
            return near;
        }
    }
}
