using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        // 아래 값은 ArtPipeline 생성 스크립트에서 나온다. 눈대중으로 맞추면 빛과 기둥이 어긋난다.
        // generate_ceiling_lamp.py 를 돌리면 실행 끝에 넣어야 할 값을 출력한다.
        const float RoomSize = 8.0f;    // 내부 한 변 (generate_cell_room.py 의 W · D)
        const float BulbY = 4.526f;     // 전구 높이 (generate_ceiling_lamp.py 의 BULB_Z)
        const float ConeAngle = 42.8f;  // 빛 원뿔 메시의 전체 벌어짐 각도

        const float EyeHeight = 1.62f;

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
            Place("CellRoom", root, Vector3.zero, Vector3.zero);
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
            Place("SlotMachine", root, Vector3.zero, Vector3.zero);
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
            light.innerSpotAngle = ConeAngle * 0.45f;
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
            // 1인칭. 플레이어는 자신을 볼 수 없다 — 손도 팔도 없어서 카메라가 곧 플레이어다.
            var go = new GameObject("PlayerCamera");
            go.tag = "MainCamera";
            // 슬롯머신(원점)에서 뒤로 물러나 정면을 본다. 이 위치는 빛 원뿔 **바깥**이다 —
            // 어둠 속에 서서 밝은 기계를 바라보는 그림이라야 고깔이 고깔로 보인다.
            go.transform.position = new Vector3(0f, EyeHeight, RoomSize * 0.32f);
            go.transform.rotation = Quaternion.Euler(6f, 180f, 0f);

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

            go.AddComponent<AudioListener>();
            // 마우스 시점. 시작 각도는 위에서 준 Transform 을 그대로 이어받는다.
            go.AddComponent<NHNAI.Game.Player.PlayerLook>();
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
