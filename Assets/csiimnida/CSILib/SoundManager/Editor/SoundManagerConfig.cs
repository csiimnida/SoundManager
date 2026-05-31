using System.IO;
using UnityEditor;

namespace csiimnida.CSILib.SoundManager.Editor
{
    /// <summary>
    /// 유저 데이터(SoundListSO, SoundSo 에셋)의 저장 경로를 관리합니다.
    /// 경로는 EditorPrefs에 저장되므로 패키지 업데이트의 영향을 받지 않습니다.
    /// </summary>
    internal static class SoundManagerConfig
    {
        private const string DataPathKey    = "SoundManager_DataPath";
        private const string DefaultDataPath = "Assets/SoundManagerData";

        public static string DataPath
        {
            get => EditorPrefs.GetString(DataPathKey, DefaultDataPath);
            set => EditorPrefs.SetString(DataPathKey, value);
        }

        public static string SoundListSOPath => $"{DataPath}/SoundListSO.asset";
        public static string SoundsFolder    => $"{DataPath}/Sounds";

        /// <summary>데이터 폴더가 AssetDatabase에 등록되어 있도록 보장합니다.</summary>
        public static void EnsureDataFolderExists() => EnsureFolderExists(DataPath);

        /// <summary>Sounds 폴더가 AssetDatabase에 등록되어 있도록 보장합니다.</summary>
        public static void EnsureSoundsFolderExists() => EnsureFolderExists(SoundsFolder);

        /// <summary>
        /// "Assets/A/B/C" 형태의 폴더 경로를 AssetDatabase API로 생성합니다.
        /// System.IO 로 만든 폴더는 AssetDatabase에 즉시 등록되지 않아
        /// CreateAsset 시 영속화 오류(assertion)를 일으키므로 반드시 이 방식을 사용합니다.
        /// </summary>
        public static void EnsureFolderExists(string folder)
        {
            folder = folder.Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || folder == "Assets") return;

            // 디스크에는 있지만 아직 임포트되지 않은 폴더를 AssetDatabase에 반영
            if (!AssetDatabase.IsValidFolder(folder) && Directory.Exists(folder))
                AssetDatabase.Refresh();

            EnsureFolderCore(folder);
        }

        private static void EnsureFolderCore(string folder)
        {
            if (folder == "Assets" || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent)) return;

            EnsureFolderCore(parent);                   // 상위 폴더부터 보장
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
