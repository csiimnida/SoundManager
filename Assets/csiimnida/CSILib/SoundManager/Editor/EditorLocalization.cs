using UnityEngine;

namespace csiimnida.CSILib.SoundManager.Editor
{
    /// <summary>
    /// SoundManager 에디터에서 사용하는 모든 텍스트를 관리하는 로컬라이제이션 클래스.
    /// 새 텍스트 추가 시 이 파일 하나만 수정하면 됩니다.
    /// PlayerPrefs 키: "SoundManagerLan" (0 = EN, 1 = KR)
    /// </summary>
    internal static class EditorLocalization
    {
        public enum Lang { EN = 0, KR = 1 }

        private static Lang _current;
        public static Lang Current => _current;
        public static bool IsKR => _current == Lang.KR;

        public static void Reload()
            => _current = (Lang)PlayerPrefs.GetInt("SoundManagerLan", 0);

        public static void Toggle()
        {
            _current = IsKR ? Lang.EN : Lang.KR;
            PlayerPrefs.SetInt("SoundManagerLan", (int)_current);
        }

        // ── 공통 ─────────────────────────────────────────────────────
        public static string Warning     => T("Warning",  "경고");
        public static string Ok          => T("Ok",       "네");
        public static string Yes         => T("Yes",      "예");
        public static string No          => T("No",       "아니오");

        // ── 다이얼로그: AudioClip ────────────────────────────────────
        public static string AudioClipNullTitle   => T("!!!Warning!!!", "!!!경고!!!");
        public static string AudioClipNullMessage => T("AudioClip cannot be null.", "AudioClip은 null일 수 없습니다.");

        // ── 다이얼로그: 이름 ─────────────────────────────────────────
        public static string NameEmptyMessage => T("Name cannot be empty!", "이름은 없을 수 없어요!");

        // ── 다이얼로그: 삭제 ─────────────────────────────────────────
        public static string DeleteTitle                    => T("Delete Sound",     "사운드 삭제");
        public static string DeleteMessage(string itemName) => T($"Delete '{itemName}'?", $"'{itemName}'을(를) 삭제하시겠습니까?");

        // ── 다이얼로그: 피치 범위 ─────────────────────────────────────
        public static string PitchMinBelowNeg3  => T("Minimum pitch must be greater than -3.", "최소 피치는 -3보다 커야 합니다.");
        public static string PitchMinAboveMax   => T("Minimum pitch must be less than the maximum pitch.", "최소 피치는 최대 피치보다 작아야 합니다.");
        public static string PitchMaxAbove3     => T("Maximum pitch must be less than 3.", "최대 피치는 3보다 작아야 합니다.");
        public static string PitchMaxBelowMin   => T("Maximum pitch must be greater than the minimum pitch.", "최대 피치는 최소 피치보다 커야 합니다.");

        // ── 버튼 텍스트 ───────────────────────────────────────────────
        public static string Play    => T("Play",    "재생");
        public static string Pause   => T("Pause",   "일시정지");
        public static string Resume  => T("Resume",  "재개");
        public static string Stop    => T("Stop",    "중지");
        public static string Create  => T("Create",  "생성");
        public static string Preview => T("Preview", "미리듣기");
        public static string LangBtn => T("Language", "언어");

        // ── 설정 패널 ─────────────────────────────────────────────
        public static string SettingsLabel   => T("Data Folder",     "데이터 폴더");
        public static string BrowseBtn       => T("Browse",          "탐색");
        public static string PathOutsideAssets => T("Please select a folder inside the project Assets folder.",
                                                    "프로젝트 Assets 폴더 안의 경로를 선택해주세요.");

        // ── UXML 필드 레이블 ─────────────────────────────────────────
        public static string LabelSoundName    => T("Sound Name",     "사운드 이름");
        public static string LabelSoundType    => T("SoundType",      "사운드 타입");
        public static string LabelAudio        => T("Audio",          "오디오");
        public static string LabelIsLoop       => T("IsLoop",         "루프");
        public static string LabelPriority     => T("Priority",       "우선순위(Priority)");
        public static string LabelVolume       => T("Volume",         "음량(Volume)");
        public static string LabelPitch        => T("Pitch",          "피치(Pitch)");
        public static string LabelStereoPan    => T("StereoPan",      "위치(StereoPan)");
        public static string LabelSpatialBlend => T("SpatialBlend",   "3D영향(SpatialBlend)");
        public static string LabelRandomPitch  => T("Random Pitch",   "랜덤 피치");
        public static string LabelMin          => T("Min",            "최소");
        public static string LabelMax          => T("Max",            "최대");

        // ── UXML 힌트 텍스트 ──────────────────────────────────────────
        public static string HintPriority =>
            T("0 = most important, 256 = least important. Default = 128.",
              "0 = 가장 중요함. 256 = 중요하지 않음. 기본값 = 128.");

        public static string HintStereoPan =>
            T("Sets the position in the stereo field of 2D sounds.",
              "2D 사운드의 스테레오 필드에서 위치를 설정합니다.");

        public static string HintSpatialBlend =>
            T("Sets how much the 3D engine has an effect on the audio source.",
              "3D 엔진이 오디오 소스에 얼마나 많은 영향을 미치는지 설정합니다.");

        // ── 섹션 제목 ─────────────────────────────────────────────────
        public static string SectionBasic    => T("Basic",       "기본 설정");
        public static string SectionAudio    => T("Audio",       "오디오 설정");
        public static string Section3D       => T("3D Settings", "3D 설정");
        public static string SectionRandom   => T("Random Pitch","랜덤 피치");
        public static string SectionFade     => T("Fade",        "페이드");
        public static string SectionCooldown => T("Cooldown",    "쿨다운");
        public static string SectionPreview  => T("Preview",     "미리듣기");

        // ── 3D / 페이드 / 쿨다운 레이블 ───────────────────────────────
        public static string LabelMinDistance => T("Min Distance", "최소 거리");
        public static string LabelMaxDistance => T("Max Distance", "최대 거리");
        public static string LabelFadeIn      => T("Fade In (s)",  "페이드 인 (초)");
        public static string LabelFadeOut     => T("Fade Out (s)", "페이드 아웃 (초)");
        public static string LabelCooldown    => T("Cooldown (s)", "쿨다운 (초)");

        public static string Hint3D =>
            T("Distance range used when SpatialBlend is greater than 0.",
              "SpatialBlend가 0보다 클 때 사용되는 거리 범위입니다.");
        public static string HintFade =>
            T("Time to ramp volume in/out. 0 means instant.",
              "음량이 오르내리는 시간입니다. 0이면 즉시 적용됩니다.");
        public static string HintCooldown =>
            T("Minimum interval before the same sound can replay. 0 means no limit.",
              "같은 사운드를 다시 재생하기까지의 최소 간격입니다. 0이면 제한 없음.");

        // ── 탭 ────────────────────────────────────────────────────────
        public static string TabSounds   => T("Sounds",   "사운드");
        public static string TabSettings => T("Settings", "설정");

        // ── Settings 탭 ───────────────────────────────────────────────
        public static string GroupPool   => T("Audio Source Pool", "오디오 소스 풀");
        public static string GroupVolume => T("Default Volumes",   "기본 음량");
        public static string GroupData   => T("Data Folder",       "데이터 폴더");

        public static string LabelPoolSize   => T("Pool Size",   "풀 크기");
        public static string LabelAutoExpand => T("Auto Expand", "자동 확장");
        public static string LabelMaster     => T("Master", "마스터");
        public static string LabelBGM        => T("BGM",    "BGM");
        public static string LabelSFX        => T("SFX",    "SFX");
        public static string SaveBtn         => T("Save",   "저장");
        public static string ResetBtn        => T("Reset",  "초기화");

        public static string HintPool =>
            T("Number of reusable AudioSources created on start. Auto Expand adds more when needed.",
              "시작 시 생성되는 재사용 AudioSource 개수입니다. 자동 확장은 부족할 때 추가로 만듭니다.");
        public static string HintVolume =>
            T("Default linear volumes saved to PlayerPrefs and applied at runtime.",
              "PlayerPrefs에 저장되어 런타임에 적용되는 기본 선형 음량입니다.");
        public static string SavedToast =>
            T("Settings saved.", "설정이 저장되었습니다.");

        // ── 빈 상태 ──────────────────────────────────────────────────
        public static string EmptyListTitle =>
            T("No sounds yet", "아직 사운드가 없습니다");
        public static string EmptyListBody =>
            T("Press + to create your first sound.", "+ 버튼을 눌러 첫 사운드를 만들어 보세요.");
        public static string SelectPrompt =>
            T("Select a sound to edit", "편집할 사운드를 선택하세요");

        // ────────────────────────────────────────────────────────────
        private static string T(string en, string kr) => IsKR ? kr : en;
    }
}
