using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NHNAI.EditorTools
{
    /// <summary>
    /// WebGL 빌드를 저장소 루트의 <c>WebGLBuild/</c> 로 뱉는다.
    ///
    /// 출력 폴더를 코드로 고정한 이유는 <c>Tools/deploy-webgl.ps1</c> 의 기본값과
    /// 짝을 맞추기 위해서다. 손으로 Build Settings 창을 쓰면 매번 폴더를 고르게 되고,
    /// 한 번 다른 곳으로 뱉으면 배포 스크립트는 예전 빌드를 조용히 올린다.
    ///
    /// 같은 함수가 메뉴와 CLI 양쪽 입구를 갖는다. 에디터가 이미 열려 있으면 메뉴가 빠르고,
    /// 닫혀 있으면 CLI 가 낫다 — 한 프로젝트를 두 인스턴스가 열 수 없어서 에디터가
    /// 열린 채로는 CLI 빌드가 잠금에 걸린다.
    ///
    /// <code>
    /// Unity.exe -quit -batchmode -logFile - -projectPath &lt;저장소 루트&gt; ^
    ///           -executeMethod NHNAI.EditorTools.WebGlBuild.BuildFromCommandLine
    /// </code>
    /// </summary>
    static class WebGlBuild
    {
        /// 저장소 루트 기준. `.gitignore` 에 들어 있어서 main 에는 담기지 않는다.
        const string OutputDirName = "WebGLBuild";

        [MenuItem("NHNAI/Build/WebGL → WebGLBuild", priority = 40)]
        static void BuildFromMenu()
        {
            var ok = Run(out var message);
            EditorUtility.DisplayDialog(ok ? "WebGL 빌드 완료" : "WebGL 빌드 실패", message, "확인");
        }

        /// <summary>
        /// CLI 입구. 배치 모드에서는 예외가 로그로만 남고 종료 코드가 0 이 되어
        /// 실패한 빌드를 배포로 넘겨 버릴 수 있다 — 그래서 종료 코드를 직접 정한다.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            if (!Run(out var message))
            {
                Debug.LogError($"[NHNAI] {message}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[NHNAI] {message}");
            EditorApplication.Exit(0);
        }

        static bool Run(out string message)
        {
            SceneBuildList.Prune();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                message = "빌드 씬 목록이 비었다.\n" +
                          "NHNAI > Scenes > 독방 (CellRoom) 을 먼저 실행한다.";
                return false;
            }

            // GitHub Pages 는 커스텀 응답 헤더를 못 준다. 압축을 켠 채 Decompression
            // Fallback 이 꺼져 있으면 로더가 `Unable to parse Build/*.br!` 로 죽는다.
            // 배포 스크립트도 같은 것을 보지만, 거기서 걸리면 빌드 시간을 이미 버린 뒤다.
            if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Disabled &&
                !PlayerSettings.WebGL.decompressionFallback)
            {
                message = $"압축이 {PlayerSettings.WebGL.compressionFormat} 인데 Decompression Fallback 이 꺼져 있다.\n\n" +
                          "Player Settings > WebGL > Publishing Settings > Decompression Fallback 을 켠다.\n" +
                          "(GitHub Pages 는 Content-Encoding 헤더를 줄 수 없다 — Tools/deploy-webgl.ps1 참조)";
                return false;
            }

            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirName));

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                // 플랫폼을 바꾸면 에셋을 WebGL 용으로 다시 임포트한다. 처음 한 번은 오래 걸린다.
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    message = "빌드 타깃을 WebGL 로 바꾸지 못했다.\n" +
                              "Unity Hub 에서 이 버전에 WebGL Build Support 모듈이 설치돼 있는지 확인한다.";
                    return false;
                }
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                message = $"빌드 결과: {summary.result} (오류 {summary.totalErrors}건)\n" +
                          "Console 에서 첫 오류부터 본다.";
                return false;
            }

            var indexHtml = Path.Combine(output, "index.html");
            if (!File.Exists(indexHtml))
            {
                // GitHub Pages 는 배포 뿌리의 index.html 로 사이트를 연다. 이게 없으면
                // 배포는 성공하는데 페이지에 404 만 뜬다.
                message = $"빌드는 끝났는데 index.html 이 없다: {indexHtml}";
                return false;
            }

            var mb = summary.totalSize / (1024f * 1024f);
            message = $"WebGL 빌드 완료 — {mb:F1} MB, {summary.totalTime:mm\\:ss}\n" +
                      $"{output}\n\n" +
                      "올리려면: .\\Tools\\deploy-webgl.ps1";
            return true;
        }
    }
}
