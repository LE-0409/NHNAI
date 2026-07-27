using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NHNAI.EditorTools
{
    /// <summary>
    /// Blender 에서 온 FBX 에 붙일 머티리얼을 만들고, FBX 가 그것을 참조하도록 리맵한다.
    ///
    /// 파이프라인 경계: ArtPipeline 은 FBX 출력까지만 책임진다. Blender 머티리얼은
    /// 미리보기 근사치일 뿐이라 엔진으로 넘어오지 않는다. 대신 슬롯 **이름**이 넘어오므로
    /// (M_Palette / M_Emissive / M_Glass / M_LightCone) 그 이름으로 여기 .mat 을 물린다.
    /// 이름은 ArtPipeline/project/palette_registry.py 의 MAT_NAME · SLOTS 가 정본이다.
    ///
    /// .mat 은 GUID 참조가 들어간 YAML 이라 손으로 쓰지 않는다 — 이 파일이 만든다.
    /// </summary>
    static class ArtMaterialLibrary
    {
        const string MaterialDir = "Assets/Art/Materials";
        const string PalettePng = "Assets/Art/Palette/palette.png";
        const string FbxDir = "Assets/Art/Environment";

        public const string Palette = "M_Palette";
        public const string Emissive = "M_Emissive";
        public const string Glass = "M_Glass";
        public const string LightCone = "M_LightCone";

        [MenuItem("NHNAI/Setup/1. 아트 머티리얼 생성 · 갱신", priority = 1)]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialDir);

            var tex = ConfigurePaletteTexture();
            CreatePalette(tex);
            CreateEmissive();
            CreateGlass(tex);
            CreateLightCone();

            AssetDatabase.SaveAssets();
            RemapFbxMaterials();

            Debug.Log($"[NHNAI] 아트 머티리얼 4종 생성 · FBX 리맵 완료 → {MaterialDir}");
        }

        /// <summary>
        /// 팔레트 텍스처는 8x8 단색 셀이고 면 UV 가 셀 '중심 한 점'에 모여 있다.
        /// 밉맵이나 압축이 켜져 있으면 인접 셀이 섞여 멀리서 엉뚱한 색이 나온다.
        /// 이 설정이 팔레트 방식이 성립하는 전제다.
        /// </summary>
        static Texture2D ConfigurePaletteTexture()
        {
            var importer = AssetImporter.GetAtPath(PalettePng) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[NHNAI] 팔레트 텍스처가 없다: {PalettePng}\n" +
                               "ArtPipeline 의 생성 스크립트를 먼저 돌려야 한다.");
                return null;
            }

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 128;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePng);
        }

        static void CreatePalette(Texture2D tex)
        {
            var mat = Ensure(Palette, "Universal Render Pipeline/Lit");
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.12f);   // 무광 — 앤틱한 표면
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
        }

        static void CreateEmissive()
        {
            // 전구. HDR 로 1을 넘겨야 Bloom 이 물린다 — 여기가 화면에서 유일하게 타는 곳이다.
            var mat = Ensure(Emissive, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f));
            mat.SetFloat("_Smoothness", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", Color.white * 6f);
            EditorUtility.SetDirty(mat);
        }

        static void CreateGlass(Texture2D tex)
        {
            var mat = Ensure(Glass, "Universal Render Pipeline/Lit");
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f, 0.22f));
            mat.SetFloat("_Smoothness", 0.85f);
            MakeTransparent(mat);
            EditorUtility.SetDirty(mat);
        }

        static void CreateLightCone()
        {
            var shader = Shader.Find("NHNAI/LightCone");
            if (shader == null)
            {
                Debug.LogError("[NHNAI] NHNAI/LightCone 셰이더를 찾을 수 없다. " +
                               "Assets/Shaders/LightCone.shader 컴파일 오류를 먼저 확인할 것.");
                return;
            }

            // _BottomY / _TopY 는 LightCone.fbx 메시의 오브젝트 공간 Y 끝값이다.
            // 눈대중으로 넣지 말 것 — generate_ceiling_lamp.py 가 실행 끝에 출력한다.
            var mat = Ensure(LightCone, "NHNAI/LightCone");
            mat.SetColor("_BaseColor", Color.white);
            // 아주 낮다. 가산 블렌딩이라 이 정도로도 어둠 위에서는 읽히고,
            // 세게 주면 흰 고깔 덩어리가 되어 '빛' 이 아니라 '물체' 가 된다.
            mat.SetFloat("_Intensity", 0.02f);
            mat.SetFloat("_BottomY", 0.02f);
            mat.SetFloat("_TopY", 4.48f);
            mat.SetFloat("_Falloff", 1.1f);
            mat.SetFloat("_BottomFade", 0.35f);

            // 경계 3종. 딱딱해 보이면 여기부터 만진다 — 원인이 서로 달라 효과가 다르다.
            // _DepthFade    바닥·벽과의 교차선 (카메라 requiresDepthTexture 가 켜져 있어야 동작)
            // _EdgeSoftness 0 = 실루엣을 녹이지 않는다. 고깔 윤곽을 살리는 쪽을 택했다
            // _RimBoost     0 = 테두리를 따로 밝히지 않는다. 세기가 낮아 강조가 필요 없다
            mat.SetFloat("_DepthFade", 1.2f);
            mat.SetFloat("_EdgeSoftness", 0f);
            mat.SetFloat("_RimBoost", 0f);
            mat.SetFloat("_RimPower", 3.0f);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>URP/Lit 을 반투명으로 바꾼다. 셰이더 키워드와 렌더 큐를 같이 건드려야 실제로 반영된다.</summary>
        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);      // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);        // Alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static Material Ensure(string name, string shaderName)
        {
            var path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName)) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader.name != shaderName)
            {
                mat.shader = Shader.Find(shaderName);
            }
            return mat;
        }

        /// <summary>
        /// FBX 가 방금 만든 .mat 을 참조하게 한다. Blender 쪽 슬롯 이름과 .mat 이름이
        /// 같아야 붙는다 — 안 붙으면 Blender 슬롯 이름이 바뀐 것이다.
        /// </summary>
        static void RemapFbxMaterials()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { FbxDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;

                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
                importer.importNormals = ModelImporterNormals.Import;   // Blender 가 구운 플랫 셰이딩 유지
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
                                                 ModelImporterMaterialSearch.Everywhere);
                importer.SaveAndReimport();
            }
        }
    }
}
