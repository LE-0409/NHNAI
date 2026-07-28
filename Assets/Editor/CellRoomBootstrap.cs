using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using NHNAI.Game.Coins;
using NHNAI.Game.Player;
using NHNAI.Game.Slot;
using NHNAI.UI.Hud;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace NHNAI.EditorTools
{
    /// <summary>
    /// 독방 씬을 만든다 — 게임이 시작되는 방.
    ///
    /// .unity 씬과 VolumeProfile.asset 은 GUID 참조가 들어간 YAML 이라 손으로 쓰지 않는다.
    /// 이 파일이 정본이고, 씬을 고치고 싶으면 여기 값을 바꿔 다시 생성한다.
    /// **씬에서 직접 만진 것은 다음 생성 때 날아간다.**
    ///
    /// 배치는 ArtPipeline 의 방 좌표계를 그대로 쓴다 (원점 = 방 바닥 중앙, Y 위쪽).
    /// 그래서 방 · 전등 · 빛 원뿔은 전부 Transform 기본값이면 맞물린다.
    /// </summary>
    static class CellRoomBootstrap
    {
        const string ScenePath = "Assets/Scenes/CellRoom.unity";
        const string ProfilePath = "Assets/Settings/CellRoomVolume.asset";
        const string FbxDir = "Assets/Art/Environment";
        const string HudUxmlPath = "Assets/UI/Screens/Hud/Hud.uxml";
        const string CoinClipPath = "Assets/Audio/CoinDispense.mp3";
        const string ReelTickClipPath = "Assets/Audio/ReelTick.mp3";
        const string CoinPickupClipPath = "Assets/Audio/CoinPickup.mp3";
        const string CoinInsertClipPath = "Assets/Audio/CoinInsert.mp3";
        const string LeverDownClipPath = "Assets/Audio/LeverDown.mp3";
        const string LeverUpClipPath = "Assets/Audio/LeverUp.mp3";
        const string WinClipPath = "Assets/Audio/WinJingle.mp3";

        // 아래 값은 ArtPipeline 생성 스크립트에서 나온다. 눈대중으로 맞추면 빛과 기둥이 어긋난다.
        // generate_ceiling_lamp.py 를 돌리면 실행 끝에 넣어야 할 값을 출력한다.
        const float RoomSize = 8.0f;    // 내부 한 변 (generate_cell_room.py 의 W · D)
        const float BulbY = 4.526f;     // 전구 높이 (generate_ceiling_lamp.py 의 BULB_Z)
        const float ConeAngle = 42.8f;  // 빛 원뿔 메시의 전체 벌어짐 각도

        /// <summary>
        /// 시점 높이(m). **시점을 조절하는 정식 손잡이는 여기다.**
        ///
        /// CharacterController 의 Center 로도 시점을 내릴 수 있지만 그건 캡슐을 통째로
        /// 들어 올려 Transform 원점을 바닥 아래로 잠기게 하는 것이라, 원점이 곧 발밑이라는
        /// 전제가 깨진다. 지금은 티가 안 나도 발소리·스폰·바닥 판정이 걸리기 시작한다.
        ///
        /// 1.25 는 성인 눈높이(약 1.6)보다 낮다. 의도한 값이다 — 슬롯머신(1.72 m)을
        /// 살짝 올려다보게 되어 기계가 사람을 내려다보는 구도가 된다.
        /// </summary>
        const float EyeHeight = 1.25f;

        /// <summary>충돌 캡슐 높이. 눈높이보다 조금 크면 된다 (머리끝 여유).</summary>
        const float BodyHeight = 1.40f;

        /// <summary>
        /// 바닥 광원 웅덩이 가장자리의 부드러움. 스포트라이트는 innerSpotAngle 부터
        /// spotAngle 까지 걸쳐 감쇠하므로, 이 비율이 낮을수록 번지는 구간이 넓어진다.
        /// 1 에 가까우면 칼로 자른 원이 된다.
        /// </summary>
        const float SpotEdgeSoftness = 0.25f;

        /// <summary>씬 조립 단계 사이로 건네는 슬롯머신 배선 묶음. 생성 중에만 산다.</summary>
        struct MachineRig
        {
            public SlotMachineView View;
            public SlotMachineWinEffect Effect;
            public CoinDispenser Dispenser;
            public Light TrayLight;
            public Transform CoinSlot;      // 흡입 판정 앵커 (원점 = 슬릿 입구)
            public Transform CoinTray;      // 트레이 조명·부근 판정 앵커 (원점 = 안쪽 바닥 중앙)
            public Transform PayoutMouth;   // 배출 스폰 앵커 (원점 = 개구 앞)
        }

        /// <summary>플레이어 쪽 배선 묶음. 동전 시스템이 카메라·충돌체를 필요로 한다.</summary>
        struct PlayerRig
        {
            public PlayerInteractor Interactor;
            public Transform CameraTransform;
            public Collider Body;           // CharacterController — 동전과의 충돌 무시용
        }

        [MenuItem("NHNAI/Scenes/독방 (CellRoom)", priority = 20)]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ArtMaterialLibrary.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var machineRig = BuildEnvironment();
            BuildLighting();
            var playerRig = BuildCamera();
            // 동전은 기계(앵커·배출기)와 플레이어(손·충돌체) 양쪽에 걸치므로 맨 뒤다.
            BuildCoins(machineRig, playerRig);
            BuildPostProcessing();
            ConfigureRenderSettings();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings(ScenePath);

            Debug.Log($"[NHNAI] 독방 씬 생성 완료 → {ScenePath}\n" +
                      "룩 조정은 Volume 프로파일과 SpotLight 세기부터 만진다.");
        }

        // --- 환경 ------------------------------------------------------------

        static MachineRig BuildEnvironment()
        {
            var root = new GameObject("Environment").transform;

            // 방 · 전등 · 빛 원뿔은 생성 스크립트가 방 좌표계로 뽑았으므로 원점에 그대로 둔다.
            var room = Place("CellRoom", root, Vector3.zero, Vector3.zero);
            // 콜라이더가 없으면 벽을 통과하고 바닥으로 떨어진다. FBX 임포트는 만들어 주지 않는다.
            if (room != null) AddMeshColliders(room);

            Place("CeilingLamp", root, Vector3.zero, Vector3.zero);

            var cone = Place("LightCone", root, Vector3.zero, Vector3.zero);
            if (cone != null)
            {
                // 빛은 물체가 아니다. 그림자를 드리우거나 받으면 즉시 정체가 들통난다.
                foreach (var r in cone.GetComponentsInChildren<Renderer>())
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }

            // 슬롯머신은 방 한가운데, 전등 바로 아래. 앞면이 +Z 를 본다
            // (ArtPipeline 규약: Blender −Y 전방 → Unity +Z 전방).
            var machine = Place("SlotMachine", root, Vector3.zero, Vector3.zero);
            if (machine == null) return default;

            // 캐비닛만 막는다. 릴은 창 안이라 닿을 일이 없고, 유리·백라이트에 콜라이더를
            // 붙이면 레버를 조준할 때 앞을 가로막는다. 레버는 아래에서 따로 붙인다.
            // LeverHub 는 레버 뿌리를 감싸는 통이라 콜라이더를 주면 레버를 노린
            // 레이를 대신 받아 조준이 먹통이 된다 — 보이기만 하면 되는 부품이다.
            // CoinSlot·PayoutMouth 는 **일부러 막는다** (skip 에 없다).
            // CoinTray 는 skip 한다 — 시각 메시(두께 1.5 cm)에 MeshCollider 를 씌우면
            // 동전 더미의 겹침 밀치기가 바닥을 관통시킨다. BuildTrayColliders 가
            // 안쪽 면은 같고 두께만 두꺼운 BoxCollider 그릇을 따로 만든다.
            // RefundButton 은 아래에서 조준 전용 트리거를 따로 받는다.
            AddMeshColliders(machine, "Reel_0", "Reel_1", "Reel_2",
                             "Lever", "LeverHub", "ReelGlass", "ReelBacklight", "RefundButton",
                             "CoinTray");
            return WireSlotMachine(machine);
        }

        /// <summary>
        /// 메시가 있는 자식마다 MeshCollider 를 붙인다. 이름이 <paramref name="skip"/> 에
        /// 있으면 건너뛴다. 정적 지오메트리라 convex 로 만들지 않는다 — 방처럼 오목한
        /// 모양을 convex 로 감싸면 속이 꽉 찬 덩어리가 되어 안으로 들어갈 수 없다.
        /// </summary>
        static void AddMeshColliders(GameObject root, params string[] skip)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                if (System.Array.IndexOf(skip, filter.gameObject.name) >= 0) continue;
                if (filter.GetComponent<Collider>() != null) continue;
                filter.gameObject.AddComponent<MeshCollider>();
            }
        }

        /// <summary>
        /// FBX 계층에서 움직이는 부품과 앵커를 찾아 컴포넌트를 붙인다.
        /// 이름은 생성 스크립트가 정한 것이라 어긋나면 조용히 동작하지 않는다 —
        /// 못 찾으면 에러를 남긴다.
        /// </summary>
        static MachineRig WireSlotMachine(GameObject machine)
        {
            var reels = new Transform[3];
            for (var i = 0; i < reels.Length; i++)
            {
                reels[i] = FindChild(machine.transform, $"Reel_{i}");
                if (reels[i] == null) Debug.LogError($"[NHNAI] SlotMachine.fbx 에 Reel_{i} 가 없다.");
            }

            var lever = FindChild(machine.transform, "Lever");
            if (lever == null)
            {
                Debug.LogError("[NHNAI] SlotMachine.fbx 에 Lever 가 없다. 상호작용을 붙이지 못했다.");
                return default;
            }

            var view = machine.AddComponent<SlotMachineView>();

            // 당첨 연출·배출기·전원은 서로 참조가 얽혀 있다 — 셋을 먼저 만들고
            // 배선은 한꺼번에 한다. 흔드는 것은 기계 루트라 릴·레버·광원이 전부
            // 같이 떤다 — 캐비닛만 떨면 부품이 공중에 남는다.
            var effect = machine.AddComponent<SlotMachineWinEffect>();
            var dispenser = machine.AddComponent<CoinDispenser>();
            var power = machine.AddComponent<SlotMachinePower>();

            var winLight = BuildWinLight(machine.transform);
            var reelLight = BuildReelBacklight(machine.transform);

            // 발광 패널의 소등은 전원 상태가 런타임 인스턴스로 처리한다. 렌더러만 넘긴다.
            var backlight = FindChild(machine.transform, "ReelBacklight");
            if (backlight == null)
                Debug.LogError("[NHNAI] SlotMachine.fbx 에 ReelBacklight 가 없다. 전원 소등이 패널을 못 끈다.");

            effect.Bind(winLight, reelLight, machine.transform, power);
            power.Bind(view, reelLight,
                       backlight != null ? backlight.GetComponent<Renderer>() : null,
                       dispenser, effect);
            var tickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ReelTickClipPath);
            if (tickClip == null)
                Debug.LogError($"[NHNAI] 릴 스핀 틱 사운드가 없다: {ReelTickClipPath}");

            var leverDownClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LeverDownClipPath);
            if (leverDownClip == null)
                Debug.LogError($"[NHNAI] 레버 당김음이 없다: {LeverDownClipPath}");

            var leverUpClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LeverUpClipPath);
            if (leverUpClip == null)
                Debug.LogError($"[NHNAI] 레버 복귀음이 없다: {LeverUpClipPath}");

            var winClip = AssetDatabase.LoadAssetAtPath<AudioClip>(WinClipPath);
            if (winClip == null)
                Debug.LogError($"[NHNAI] 승리 징글이 없다: {WinClipPath}");

            view.Bind(reels, lever, effect, dispenser, tickClip, leverDownClip, leverUpClip, winClip);
            // dispenser 의 앵커·템플릿·트레이 조명은 BuildCoins 단계가 꽂는다.

            var rig = new MachineRig
            {
                View = view,
                Effect = effect,
                Dispenser = dispenser,
                TrayLight = BuildTrayLight(machine.transform),
                CoinSlot = FindAnchor(machine, "CoinSlot"),
                CoinTray = FindAnchor(machine, "CoinTray"),
                PayoutMouth = FindAnchor(machine, "PayoutMouth"),
            };

            // 레이캐스트로 잡으려면 Collider 가 있어야 하고, Interactable 과 **같은
            // 오브젝트**에 있어야 한다. FBX 임포트는 Collider 를 만들어 주지 않는다.
            //
            // 메시 모양 그대로 감싸지 않고 넉넉한 캡슐을 씌운다. 레버 팔은 반경 0.016 m 라
            // 실루엣대로 잡으면 조준이 바늘구멍이 된다. 조준 판정은 보이는 모양이 아니라
            // '노리기 쉬운 크기' 로 잡는 게 맞다.
            //
            // 캡슐은 레버 로컬 +Y 를 따라 선다. 팔이 곧게 서 있으므로 눕힌 만큼의
            // z 보정이 필요 없다 — 생성 스크립트의 LEVER_TILT 를 0 이 아니게 바꾸면
            // 여기 center.z 도 같이 옮겨야 캡슐이 팔을 벗어나지 않는다.
            //
            // 레버가 캐비닛 앞면(z=+0.26)보다 앞으로 나왔으므로 캡슐도 앞으로 튀어나온다.
            // 기계 오른쪽 끝 4 cm 쯤(창 오른쪽 프레임)을 조준하면 캐비닛 대신 레버가
            // 잡힌다. 그 자리엔 어차피 다른 상호작용이 없어서 손해가 아니라 이득이다.
            //
            // 트리거 = 조준 전용 (PlayerInteractor 의 규약). 실루엣보다 훨씬 큰 캡슐이라
            // 물리에 남겨 두면 보이지 않는 벽이 된다 — 들고 있는 동전의 손 위치 보정
            // 레이가 걸려 동전이 코앞으로 딸려 오고, 낙하 동전이 허공에 얹힌다.
            // 조준 레이(Collide)만 이 캡슐을 보고, 물리와 손 보정 레이(Ignore)는 통과한다.
            var capsule = lever.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 1;                                   // Y 축
            capsule.height = 0.42f;
            capsule.radius = 0.09f;
            capsule.center = new Vector3(0.01f, 0.15f, 0f);
            capsule.isTrigger = true;

            lever.gameObject.AddComponent<SlotMachineLever>().Bind(view);

            // 환불 버튼 — 조작대 왼쪽. 실물(반지름 0.028)보다 넉넉한 조준 박스를 씌운다.
            // 레버와 같은 이유로 트리거다. 원점이 버튼 중심이라 center 는 거의 0 이다.
            var refundButton = FindChild(machine.transform, "RefundButton");
            if (refundButton == null)
            {
                Debug.LogError("[NHNAI] SlotMachine.fbx 에 RefundButton 이 없다. " +
                               "ArtPipeline 의 generate_slot_machine.py 를 다시 돌려야 한다.");
            }
            else
            {
                var box = refundButton.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.10f, 0.06f, 0.10f);
                box.center = new Vector3(0f, 0.01f, 0f);
                box.isTrigger = true;
                refundButton.gameObject.AddComponent<SlotMachineRefundButton>().Bind(view);
            }

            // 트레이 물리 그릇. AddMeshColliders 가 CoinTray 를 skip 한 자리를 채운다.
            if (rig.CoinTray != null) BuildTrayColliders(rig.CoinTray);

            return rig;
        }

        /// <summary>
        /// 트레이의 물리 그릇. 시각 메시(generate_slot_machine.py, 두께 1.5 cm)에
        /// MeshCollider 를 그대로 씌우면 두 가지가 터진다:
        /// ① 소나기로 쌓인 동전 더미의 겹침 해소(depenetration)가 한 스텝에 바닥
        ///    두께 이상을 밀어내 동전이 바닥 밑으로 빠진다.
        /// ② non-convex MeshCollider 는 삼각형 표면 판정뿐이라, 일단 바닥 슬래브
        ///    '안'에 들어간 동전은 어느 쪽으로 밀려날지 보장이 없어 바닥 속에 갇혀 떤다.
        /// 그래서 물리만 두꺼운 BoxCollider 로 짠다. 동전이 닿는 안쪽 면과 벽 윗면은
        /// 시각 메시와 정확히 일치시키고(넘침 연출 유지), 두께는 아래로만 키운다 —
        /// 겉보기와 조준 실루엣은 그대로다. Box 는 부피 판정이라 ②도 함께 사라진다.
        /// </summary>
        static void BuildTrayColliders(Transform tray)
        {
            // generate_slot_machine.py 의 TRAY_* 와 같은 값. 트레이 로컬 좌표계:
            // 원점 = 안쪽 바닥 중앙, +z = 플레이어 쪽(앞벽), -z 끝 = 캐비닛 앞면(뒷벽 겸용).
            const float W = 0.36f;       // 바깥 폭 (TRAY_W)
            const float D = 0.16f;       // 안쪽 깊이 (TRAY_D) — 캐비닛 앞면 ~ 앞벽 안쪽 면
            const float T = 0.015f;      // 시각 벽 두께 (TRAY_T) — 벽 콜라이더는 이대로
            const float WallH = 0.075f;  // 안쪽 바닥 기준 벽 높이 (TRAY_WALL) — 넘침 연출 유지
            // 물리 바닥 두께. Coin.MaxDepenetrationVelocity 상한(스텝당 1 cm)으로는
            // 못 넘는 여유이면서, 시각 바닥(1.5 cm) + 받침(y −0.04 까지)에 거의 감춰진다.
            const float FloorThick = 0.05f;

            var physics = new GameObject("TrayPhysics");
            physics.transform.SetParent(tray, false);

            // 바닥 — 윗면(y=0)은 시각 바닥과 같고 아래로만 두껍다. 뒤로 2 cm 는
            // 캐비닛 속에 잠겨 캐비닛 앞면 콜라이더와의 틈새를 막는다.
            AddBox(physics, new Vector3(0f, -FloorThick / 2f, -0.0025f),
                            new Vector3(W, FloorThick, D + T + 0.02f));
            // 앞벽 — 안쪽 면 z = D/2. 위는 시각과 같은 높이, 아래는 바닥 박스와 겹쳐
            // 이음매 틈을 없앤다.
            AddBox(physics, new Vector3(0f, (WallH - FloorThick) / 2f, (D + T) / 2f),
                            new Vector3(W, WallH + FloorThick, T));
            // 옆벽 좌·우 — 안쪽 면 x = ±(W/2 − T). 앞벽 바깥면(z = D/2 + T)까지 이어
            // 모서리 틈을 없앤다. 시각적으로도 그 구간은 앞벽이 차지하고 있다.
            AddBox(physics, new Vector3(-(W - T) / 2f, (WallH - FloorThick) / 2f, T / 2f),
                            new Vector3(T, WallH + FloorThick, D + T));
            AddBox(physics, new Vector3((W - T) / 2f, (WallH - FloorThick) / 2f, T / 2f),
                            new Vector3(T, WallH + FloorThick, D + T));
        }

        static void AddBox(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        /// <summary>동전 앵커 오브젝트. 원점이 곧 앵커라 Transform 만 있으면 된다.</summary>
        static Transform FindAnchor(GameObject machine, string name)
        {
            var anchor = FindChild(machine.transform, name);
            if (anchor == null)
                Debug.LogError($"[NHNAI] SlotMachine.fbx 에 {name} 이 없다. " +
                               "ArtPipeline 의 generate_slot_machine.py 를 다시 돌려야 한다.");
            return anchor;
        }

        /// <summary>
        /// 성공했을 때 방을 물들이는 광원. **평소에는 세기 0 으로 꺼져 있다** —
        /// 켜 두면 기계가 방을 밝히는 그림이 되어 어둠이 무너진다.
        ///
        /// 자리는 기계 앞 · 마퀴 높이다. 여기서 켜지면 마퀴 패널(paper)과 조작대가
        /// 먼저 밝아지고 그 빛이 벽 · 바닥으로 번져, 성공이 기계에서 시작해 방으로
        /// 퍼지는 순서로 읽힌다. range 는 방 한 변(8 m)에 못 미치게 두되 벽까지는
        /// 닿게 한다 — 짧으면 기계 주변만 반짝이고 방은 그대로 어둡다.
        ///
        /// 그림자는 끈다. 켜면 광원이 기계 코앞이라 자기 그림자가 캐비닛 앞면을 갉는다.
        /// </summary>
        static Light BuildWinLight(Transform machine)
        {
            var go = new GameObject("WinLight");
            go.transform.SetParent(machine, false);
            go.transform.localPosition = new Vector3(0f, 1.62f, 0.34f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.intensity = 0f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>
        /// 릴 창 조명. 발광 머티리얼은 스스로 빛나 보일 뿐 주변을 밝히지 않아서,
        /// 실제로 릴을 비추려면 광원이 따로 필요하다. 기계가 켜져 있다는 인상이 여기서 나온다.
        ///
        /// ⚠️ **거리에 극도로 민감하다.** 릴 앞면이 z=+0.10 에 있어서, 처음에 광원을
        /// z=+0.12 · y=1.18 에 두었더니 표면까지 2 cm 라 감쇠가 거의 없이 최대 밝기가
        /// 그대로 꽂혀 창 안이 새하얗게 날아갔다. 세기를 만지기 전에 **거리부터** 본다.
        ///
        /// 지금 위치는 릴 창 위(마퀴 높이)다. 릴까지 0.37 m 로 떨어지고 위에서 비춰
        /// 입사각도 눕는다 — 두 효과가 겹쳐 적당해진다.
        ///
        /// 이 배치는 그림자를 끈 것에 기대고 있다. 광원이 릴 격실 밖에 있어서,
        /// 그림자를 켜면 프레임에 막혀 릴이 캄캄해진다. 그림자가 필요해지면
        /// 광원을 격실 안으로 되돌리고 세기를 크게 낮춰야 한다.
        /// </summary>
        static Light BuildReelBacklight(Transform machine)
        {
            var go = new GameObject("ReelBacklight_Light");
            go.transform.SetParent(machine, false);
            go.transform.localPosition = new Vector3(0f, 1.55f, 0.12f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            // 꺼진 채 시작한다 — 게임은 크레딧 0 으로 열리고, 기계는 죽어 있어야 한다.
            // '쉴 때' 세기의 정본은 SlotMachinePower.reelRestIntensity 다. 여기 아니다.
            light.intensity = 0f;
            light.range = 0.75f;          // 창 밖으로 새어 방을 밝히지 않을 만큼만
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>
        /// 배출 트레이를 내리비추는 조명. 배출된 동전이 **보여야** 주울 수 있다 —
        /// 방의 전등은 기계 위에 있어서 트레이는 캐비닛 그늘에 잠긴다.
        ///
        /// 평소엔 꺼져 있고 CoinDispenser 가 켠다: 배출 중 + 트레이에 동전이 남아 있는
        /// 동안. 전원 상태와는 독립이다 — 크레딧이 떨어졌다고 딴 돈까지 캄캄해지면 안 된다.
        ///
        /// 스폿을 수직 아래로 쏜다. 트레이 안쪽 바닥(y 0.20)까지 0.42 m 라 60도 원뿔이면
        /// 바닥에서 반경 약 0.24 m — 트레이(반폭 0.18)를 덮고 조금 넘쳐 앞 바닥에
        /// 빛 웅덩이가 진다. 그 웅덩이가 "여기 돈이 나온다" 는 표지판 역할을 한다.
        /// </summary>
        static Light BuildTrayLight(Transform machine)
        {
            var go = new GameObject("TrayLight");
            go.transform.SetParent(machine, false);
            go.transform.localPosition = new Vector3(0f, 0.62f, 0.36f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 수직 아래

            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = Color.white;
            light.intensity = 0f;         // CoinDispenser 가 켠다
            light.range = 1.0f;
            light.spotAngle = 60f;
            light.innerSpotAngle = 30f;
            light.shadows = LightShadows.None;
            return light;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static GameObject Place(string fbxName, Transform parent, Vector3 pos, Vector3 euler)
        {
            var path = $"{FbxDir}/{fbxName}.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                Debug.LogError($"[NHNAI] FBX 가 없다: {path}\n" +
                               $"ArtPipeline 에서 generate_*.py 를 먼저 돌려야 한다.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            go.name = fbxName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            return go;
        }

        // --- 조명 ------------------------------------------------------------

        static void BuildLighting()
        {
            // 어둠은 알베도가 아니라 조명으로 만든다. 에셋을 어둡게 칠해 어둠을 흉내내면
            // 빛이 닿는 곳까지 탁해져서, 밝은 곳과 어두운 곳의 대비가 사라진다.
            var go = new GameObject("SpotLight_Bulb");
            go.transform.position = new Vector3(0f, BulbY, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 수직 아래

            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = Color.white;
            // 전등이 바닥에서 4.5 m 로 높아졌다. 감쇠가 거리 제곱이라 3 m 짜리 방에서 쓰던
            // 세기를 그대로 두면 바닥이 거의 안 밝다.
            light.intensity = 45f;
            light.range = 12f;
            // 빛 원뿔 **메시**의 벌어짐과 같은 각도여야 빛과 기둥이 따로 놀지 않는다.
            light.spotAngle = ConeAngle;
            light.innerSpotAngle = ConeAngle * SpotEdgeSoftness;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            light.shadowBias = 0.02f;
            light.shadowNormalBias = 0.2f;
        }

        static void ConfigureRenderSettings()
        {
            // 스카이박스가 남아 있으면 방 전체가 하늘빛으로 들려 어둠이 성립하지 않는다.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.015f, 0.015f, 0.018f);
            RenderSettings.reflectionIntensity = 0f;

            // 안개는 '벽이 있는지도 모를 정도로 어둡다' 를 만드는 장치다.
            // 빛이 닿지 않는 벽을 검정으로 녹여 방의 경계를 지운다.
            // 방이 8 m 로 넓어져 거리가 길어진 만큼 밀도는 낮춘다. 같은 값을 두면
            // 빛 원뿔 주변까지 뭉개져 고깔 형태가 안 보인다.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = 0.085f;
        }

        // --- 카메라 ----------------------------------------------------------

        static PlayerRig BuildCamera()
        {
            // 1인칭. 플레이어는 자신을 볼 수 없어서 보이는 몸은 없지만, 걷고 부딪히려면
            // 충돌체가 필요하다. **몸통이 좌우(yaw)를 맡고 카메라가 상하(pitch)를 맡는다** —
            // 카메라만 돌리면 앞으로 걸을 때 보는 방향과 다른 데로 간다.
            var body = new GameObject("Player");
            // 슬롯머신(원점)에서 뒤로 물러나 정면을 본다. 이 위치는 빛 원뿔 **바깥**이다 —
            // 어둠 속에 서서 밝은 기계를 바라보는 그림이라야 고깔이 고깔로 보인다.
            // 바닥에 살짝 띄워 시작한다. 정확히 0 이면 첫 프레임에 바닥을 파고든 판정이 난다.
            body.transform.position = new Vector3(0f, 0.05f, RoomSize * 0.20f);
            body.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var controller = body.AddComponent<CharacterController>();
            controller.height = BodyHeight;
            controller.radius = 0.28f;
            // ⚠️ 이 식을 깨지 않는다. 캡슐 바닥이 원점에 와야 Transform 의 Y 가 곧
            // 발밑 높이다. 시점을 낮추고 싶으면 여기가 아니라 EyeHeight 를 만진다.
            controller.center = new Vector3(0f, controller.height / 2f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;

            body.AddComponent<PlayerMove>();

            var go = new GameObject("PlayerCamera");
            go.tag = "MainCamera";
            go.transform.SetParent(body.transform, false);
            go.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            // 숙이지 않는다. 눈높이 1.25 면 릴 창(1.03~1.33)이 거의 정면에 온다 —
            // 눈이 높았을 때는 숙여야 기계 하단이 들어왔지만 이제는 그럴 필요가 없다.
            go.transform.localRotation = Quaternion.identity;

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;

            // Camera 를 붙이면 URP 가 이 컴포넌트를 자동으로 같이 붙이는 경우가 있다.
            // DisallowMultipleComponent 라 무조건 AddComponent 하면 실패한다.
            var data = go.GetComponent<UniversalAdditionalCameraData>()
                       ?? go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            // LightCone 셰이더의 _DepthFade 가 이걸 읽는다. 끄면 빛 기둥이 바닥·벽을
            // 관통하며 딱딱한 교차선을 남긴다.
            data.requiresDepthTexture = true;

            go.AddComponent<AudioListener>();
            // 마우스 시점. 좌우는 몸통에, 상하는 카메라에 건다.
            go.AddComponent<PlayerLook>().Bind(body.transform);

            // 화면 중앙 조준 + 클릭. HUD 가 이 컴포넌트를 구독한다.
            var interactor = go.AddComponent<PlayerInteractor>();
            BuildHud(interactor);

            return new PlayerRig
            {
                Interactor = interactor,
                CameraTransform = go.transform,
                Body = controller,
            };
        }

        // --- 동전 -------------------------------------------------------------

        const string CoinPhysicsPath = "Assets/Settings/CoinPhysics.physicMaterial";

        /// <summary>
        /// 동전 시스템 배선 — 손(캐리어) · 물리 머티리얼 · 씬의 비활성 템플릿 · 시작 동전 3개.
        /// 기계(앵커·배출기)와 플레이어(카메라·충돌체) 양쪽이 다 만들어진 뒤에 불린다.
        ///
        /// 템플릿을 .prefab 으로 굽지 않고 씬에 비활성으로 심는 이유: 씬의 정본이
        /// 이 파일이듯 동전의 정본도 이 함수다. 에셋을 하나 더 만들면 정본이 갈라진다.
        /// </summary>
        static void BuildCoins(MachineRig machine, PlayerRig player)
        {
            if (player.CameraTransform == null) return;

            var pickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CoinPickupClipPath);
            if (pickupClip == null)
                Debug.LogError($"[NHNAI] 동전 픽업음이 없다: {CoinPickupClipPath}");

            var insertClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CoinInsertClipPath);
            if (insertClip == null)
                Debug.LogError($"[NHNAI] 동전 투입음이 없다: {CoinInsertClipPath}");

            // 잡은 동전을 들고 다니는 손. 카메라에 붙는다 — holdOffset 이 카메라 로컬이다.
            var carrier = player.CameraTransform.gameObject.AddComponent<PlayerCoinCarrier>();
            carrier.Bind(player.Interactor, machine.View, machine.CoinSlot, pickupClip, insertClip);

            var physics = BuildCoinPhysicsMaterial();

            var root = new GameObject("Coins").transform;
            var template = Place("Coin", root, Vector3.zero, Vector3.zero);
            if (template == null) return;
            template.name = "CoinTemplate";

            // Coin.fbx 는 오브젝트 하나라 보통 루트에 메시가 있지만, 임포터가 계층을
            // 어떻게 짜든 규약(Interactable = Collider 와 같은 오브젝트)이 지켜지도록
            // 메시가 있는 오브젝트를 찾아 전부 거기에 붙인다.
            var filter = template.GetComponentInChildren<MeshFilter>(true);
            if (filter == null)
            {
                Debug.LogError("[NHNAI] Coin.fbx 에 메시가 없다. 동전을 만들지 못했다.");
                return;
            }
            var target = filter.gameObject;

            var body = target.AddComponent<Rigidbody>();
            body.mass = Coin.Mass;
            // 얇고 작고 빠르다 — 이산 판정이면 트레이 바닥을 뚫는 프레임이 나온다.
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // CCD 는 '날아가다 뚫는 것' 만 막는다. 더미로 쌓여 겹친 동전을 솔버가
            // 밀쳐낼 때의 순간이동은 이 상한이 막는다 — 값의 근거는 Coin 쪽 주석.
            body.maxDepenetrationVelocity = Coin.MaxDepenetrationVelocity;
            // 코앞에서 들여다보는 오브젝트라 물리 스텝(50 Hz)과 렌더 프레임 사이
            // 계단 현상이 보인다. 보간으로 잇는다.
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var collider = target.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;   // 12각 원반 그대로 — 실루엣만큼 타이트
            collider.convex = true;                    // Rigidbody 가 붙는 콜라이더는 convex 여야 한다
            collider.sharedMaterial = physics;
            collider.contactOffset = Coin.ContactOffset;

            target.AddComponent<Coin>().Bind(carrier, player.Body);

            // 템플릿은 보이지 않는다. 배출기와 아래 시작 동전이 복제해 쓴다.
            template.SetActive(false);

            var coinClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CoinClipPath);
            if (coinClip == null)
                Debug.LogError($"[NHNAI] 동전 배출음이 없다: {CoinClipPath}");

            if (machine.Dispenser != null)
                machine.Dispenser.Bind(template, machine.PayoutMouth, machine.CoinTray,
                                       machine.TrayLight, machine.Effect, coinClip);

            // 시작 동전 3개 — 스폰(0, 0.05, 1.6)에서 기계로 걷는 동선 좌우, 빛 원뿔
            // (바닥 반경 ~1.77 m) 안. 바닥에 떨어진 동전을 주워 넣는 첫 행동을
            // 조명이 가르쳐 준다. 첫 스핀이 꽝이어도 두 번 더 기회가 있다.
            SpawnStartCoin(template, root, new Vector3(0.45f, 0.03f, 0.95f), 20f);
            SpawnStartCoin(template, root, new Vector3(-0.55f, 0.03f, 1.15f), 130f);
            SpawnStartCoin(template, root, new Vector3(0.10f, 0.03f, 0.60f), 275f);
        }

        static void SpawnStartCoin(GameObject template, Transform parent, Vector3 pos, float yaw)
        {
            var coin = Object.Instantiate(template, parent);
            coin.name = "Coin";
            // 바닥에서 3 cm — 첫 물리 프레임에 살짝 내려앉으며 자연스럽게 자리 잡는다.
            coin.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            coin.SetActive(true);
        }

        /// <summary>
        /// 동전 물리 머티리얼. VolumeProfile 처럼 에셋으로 굽되 값의 정본은 Coin.cs 상수다.
        ///
        /// bounceCombine 이 Maximum 인 이유: 바닥·기계에는 물리 머티리얼이 없어(반발 0)
        /// 기본 조합(평균)이면 동전의 튕김이 반토막 난다. 큰 쪽을 쓰면 동전끼리든
        /// 바닥에든 Coin.Bounciness 가 그대로 산다.
        /// </summary>
        static PhysicsMaterial BuildCoinPhysicsMaterial()
        {
            var material = new PhysicsMaterial("CoinPhysics")
            {
                bounciness = Coin.Bounciness,
                dynamicFriction = Coin.Friction,
                staticFriction = Coin.Friction,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(CoinPhysicsPath)!);
            AssetDatabase.DeleteAsset(CoinPhysicsPath);
            AssetDatabase.CreateAsset(material, CoinPhysicsPath);
            return material;
        }

        // --- HUD --------------------------------------------------------------

        static void BuildHud(PlayerInteractor interactor)
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            if (uxml == null)
            {
                Debug.LogError($"[NHNAI] HUD UXML 이 없다: {HudUxmlPath}");
                return;
            }

            var go = new GameObject("Hud");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = UiBootstrap.Build();
            doc.visualTreeAsset = uxml;

            go.AddComponent<HudScreen>().Bind(interactor);
        }

        // --- 포스트 프로세싱 --------------------------------------------------

        static void BuildPostProcessing()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "CellRoomVolume";

            // 흑백은 여기서 만든다. 팔레트는 이미 무채색이지만 채도를 한 번 더 죽여
            // 나중에 색 있는 요소가 들어와도 화면이 흑백을 유지하게 한다.
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-100f);
            color.contrast.Override(18f);
            color.postExposure.Override(0.2f);

            // 앤틱한 필름 톤. 하이라이트를 부드럽게 눕혀 전구가 하얗게 뭉개지지 않게 한다.
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            // 전구와 밝은 금속만 타야 한다. 임계값을 낮추면 방 전체가 뿌예진다.
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.72f);

            // 화면 네 귀퉁이를 눌러 시선을 전등 아래로 모은다.
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.45f);

            // 오래된 필름 입자. 어두운 화면의 밴딩을 덮는 실용적 효과이기도 하다.
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.55f);
            grain.response.Override(0.75f);

            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath)!);
            AssetDatabase.DeleteAsset(ProfilePath);
            AssetDatabase.CreateAsset(profile, ProfilePath);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();

            var go = new GameObject("GlobalVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        }

        // --- 빌드 설정 --------------------------------------------------------

        static void RegisterInBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
