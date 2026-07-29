using System.IO;
using System.Linq;
using UnityEditor;

namespace NHNAI.EditorTools
{
    /// <summary>
    /// 빌드에 들어가는 씬 목록. <c>ProjectSettings/EditorBuildSettings.asset</c> 은
    /// 손으로 고치지 않는다 (CLAUDE.md) — 씬을 만드는 부트스트랩이 여기를 거쳐 등록한다.
    ///
    /// **순서가 의미를 갖는다.** 빌드된 게임은 0번 씬으로 열린다. 그래서 메인메뉴는
    /// 자리를 지정해 넣고(<c>index: 0</c>), 나머지는 뒤에 붙인다.
    /// </summary>
    static class SceneBuildList
    {
        /// <summary>
        /// 씬을 목록에 넣고 켠다. 이미 있으면 켜기만 한다.
        /// <paramref name="index"/> 를 주면 그 자리로 옮긴다 (음수면 순서를 건드리지 않는다).
        /// </summary>
        public static void Ensure(string scenePath, int index = -1)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var found = scenes.FindIndex(s => s.path == scenePath);

            EditorBuildSettingsScene entry;
            if (found >= 0)
            {
                entry = scenes[found];
                entry.enabled = true;

                // 자리를 지정하지 않았거나 이미 그 자리면 켜는 것으로 끝난다.
                if (index < 0 || found == index)
                {
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
                scenes.RemoveAt(found);
            }
            else
            {
                entry = new EditorBuildSettingsScene(scenePath, true);
            }

            if (index < 0 || index > scenes.Count) scenes.Add(entry);
            else scenes.Insert(index, entry);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>목록에 켜진 채로 들어 있는가. 씬 이름으로 여는 코드가 이걸 전제한다.</summary>
        public static bool Contains(string scenePath) =>
            EditorBuildSettings.scenes.Any(s => s.path == scenePath && s.enabled);

        /// <summary>
        /// 파일이 사라진 씬을 목록에서 뺀다. 씬을 지워도 목록의 항목은 남아 있고,
        /// 그 항목이 0번을 차지하고 있으면 빌드가 존재하지 않는 씬으로 열린다.
        /// 부트스트랩이 씬을 만들 때마다 부른다 — 목록이 스스로 정리된다.
        /// </summary>
        public static void Prune()
        {
            var alive = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToArray();
            if (alive.Length == EditorBuildSettings.scenes.Length) return;
            EditorBuildSettings.scenes = alive;
        }
    }
}
