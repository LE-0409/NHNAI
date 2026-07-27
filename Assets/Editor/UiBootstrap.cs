using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.EditorTools
{
    /// <summary>
    /// UI Toolkit 런타임 기반. PanelSettings 는 GUID 참조가 들어간 YAML 이라
    /// 손으로 쓰지 않고 여기서 만든다.
    /// </summary>
    static class UiBootstrap
    {
        const string PanelPath = "Assets/Settings/UI/GamePanelSettings.asset";
        const string ThemePath = "Assets/UI/Theme/GameTheme.tss";

        [MenuItem("NHNAI/Setup/2. PanelSettings 생성 · 갱신", priority = 2)]
        public static PanelSettings Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PanelPath)!);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelPath);
            }

            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            // landscape 고정이라 가로·세로 어느 쪽에 맞출지 한쪽으로 기울일 이유가 없다.
            panel.match = 0.5f;
            panel.scale = 1f;
            panel.targetTexture = null;

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
                Debug.LogError($"[NHNAI] 런타임 테마가 없다: {ThemePath}\n" +
                               "테마가 없으면 UI 가 스타일 없이 그려진다.");
            else
                panel.themeStyleSheet = theme;

            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }
    }
}
