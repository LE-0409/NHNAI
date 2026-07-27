using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        [MenuItem("NHNAI/Scenes/독방 (CellRoom)", priority = 20)]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ArtMaterialLibrary.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();
            BuildLighting();
            BuildCamera();
            BuildPostProcessing();
            ConfigureRenderSettings();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings(ScenePath);

            Debug.Log($"[NHNAI] 독방 씬 생성 완료 → {ScenePath}\n" +
                      "룩 조정은 Volume 프로파일과 SpotLight 세기부터 만진다.");
        }

        // --- 환경 ------------------------------------------------------------

        static void BuildEnvironment()
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
            if (machine != null)
            {
                // 캐비닛만 막는다. 릴은 창 안이라 닿을 일이 없고, 유리·백라이트에 콜라이더를
                // 붙이면 레버를 조준할 때 앞을 가로막는다. 레버는 아래에서 따로 붙인다.
                AddMeshColliders(machine, "Reel_0", "Reel_1", "Reel_2",
                                 "Lever", "ReelGlass", "ReelBacklight");
                WireSlotMachine(machine);
            }
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
        /// FBX 계층에서 움직이는 부품을 찾아 컴포넌트를 붙인다.
        /// 이름은 생성 스크립트가 정한 것이라 어긋나면 조용히 동작하지 않는다 —
        /// 못 찾으면 에러를 남긴다.
        /// </summary>
        static void WireSlotMachine(GameObject machine)
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
                return;
            }

            var view = machine.AddComponent<SlotMachineView>();
            view.Bind(reels, lever);

            // 레이캐스트로 잡으려면 Collider 가 있어야 하고, Interactable 과 **같은
            // 오브젝트**에 있어야 한다. FBX 임포트는 Collider 를 만들어 주지 않는다.
            //
            // 메시 모양 그대로 감싸지 않고 넉넉한 캡슐을 씌운다. 레버 팔은 반경 0.016 m 라
            // 실루엣대로 잡으면 조준이 바늘구멍이 된다. 조준 판정은 보이는 모양이 아니라
            // '노리기 쉬운 크기' 로 잡는 게 맞다.
            //
            // 캡슐은 레버 로컬 +Y 를 따라 선다 — 팔이 위로 뻗고 뒤(−Z)로 조금 눕는다.
            // 캐비닛 앞면(z=+0.26)이 이 캡슐보다 플레이어에 가까워서, 앞에서 캐비닛을
            // 조준했을 때 레버가 잘못 잡히지는 않는다.
            var capsule = lever.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 1;                                   // Y 축
            capsule.height = 0.42f;
            capsule.radius = 0.09f;
            capsule.center = new Vector3(0.01f, 0.15f, -0.05f);

            lever.gameObject.AddComponent<SlotMachineLever>().Bind(view);

            BuildReelBacklight(machine.transform);
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
        static void BuildReelBacklight(Transform machine)
        {
            var go = new GameObject("ReelBacklight_Light");
            go.transform.SetParent(machine, false);
            go.transform.localPosition = new Vector3(0f, 1.55f, 0.12f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.intensity = 1.6f;
            light.range = 0.75f;          // 창 밖으로 새어 방을 밝히지 않을 만큼만
            light.shadows = LightShadows.None;
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

        static void BuildCamera()
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
