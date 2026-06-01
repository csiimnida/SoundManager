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
        public static string GroupSceneManager => T("Scene SoundManager", "씬 SoundManager");
        public static string GroupMixerParams  => T("Mixer Exposed Parameters", "믹서 Exposed 파라미터");

        public static string LabelSoundListSo => T("Sound List SO", "Sound List SO");
        public static string LabelAudioMixer  => T("Audio Mixer", "Audio Mixer");
        public static string LabelBgmGroup    => T("BGM Group", "BGM 그룹");
        public static string LabelSfxGroup    => T("SFX Group", "SFX 그룹");
        public static string LabelMasterParam => T("Master Volume Param", "마스터 음량 파라미터");
        public static string LabelBgmParam    => T("BGM Volume Param", "BGM 음량 파라미터");
        public static string LabelSfxParam    => T("SFX Volume Param", "SFX 음량 파라미터");

        public static string SyncSoundListBtn     => T("Use Editor Sound List", "에디터 Sound List 적용");
        public static string CreateSceneManagerBtn => T("Create SoundManager in Scene", "씬에 SoundManager 생성");
        public static string SceneTargetLabel      => T("Editing", "편집 대상");

        public static string HintSceneManager => T(
            "Same fields as the SoundManager component in the scene. Required for Play Mode playback.",
            "씬에 있는 SoundManager 컴포넌트와 동일한 설정입니다. Play Mode 재생에 필요합니다.");
        public static string NoSceneManager => T(
            "No SoundManager in loaded scenes. Create one or open a scene that contains it.",
            "로드된 씬에 SoundManager가 없습니다. 생성하거나 해당 오브젝트가 있는 씬을 여세요.");
        public static string MultipleSceneManagers => T(
            "Multiple SoundManagers found. Settings apply to the first one; use Sync to update all Sound List SO references.",
            "SoundManager가 여러 개 있습니다. 첫 번째만 편집되며, Sync로 Sound List SO는 모두 갱신할 수 있습니다.");

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

        // ── 고급 설정 창 ──────────────────────────────────────────────
        public static string AdvancedTitle   => T("Advanced Sound Settings", "고급 사운드 설정");
        public static string AdvancedBtn     => T("Advanced Settings", "고급 설정");

        public static string AdvResetBtn   => T("Reset to Default", "기본값으로 초기화");
        public static string AdvCloseBtn   => T("Close", "닫기");

        // 시작 지점
        public static string AdvSectionStart   => T("Playback Start", "재생 시작 지점");
        public static string AdvStartOffset    => T("Skip Intro (s)", "앞부분 건너뛰기 (초)");
        public static string AdvRandomStart    => T("Random Start Position", "랜덤 시작 위치");
        public static string AdvPlayFromStart  => T("Play from Start Point", "시작 지점부터 재생");
        public static string AdvStartHint      => T(
            "Drag the blue marker on the waveform, or type a value. Playback begins from here.\nTurn on Random Start to begin at a random spot each time.",
            "파형 위의 파란 마커를 드래그하거나 값을 입력하세요. 재생이 이 지점부터 시작됩니다.\n'랜덤 시작 위치'를 켜면 매번 임의 지점에서 시작합니다.");

        // 클립 배리에이션
        public static string AdvSectionVariation => T("Clip Variations", "클립 배리에이션");
        public static string AdvAddClip          => T("+ Add Clip", "+ 클립 추가");
        public static string AdvVariationHint    => T(
            "Each play picks a random clip from the main clip + this list. Great for footsteps, hits, gunshots.",
            "재생할 때마다 기본 클립 + 아래 목록 중 하나를 무작위로 재생합니다. 발소리·타격음·총성 등에 좋습니다.");

        // 동시 재생 (보이스)
        public static string AdvSectionVoices  => T("Concurrent Playback", "동시 재생");
        public static string AdvMaxVoices      => T("Max Voices (0 = unlimited)", "최대 동시 개수 (0 = 무제한)");
        public static string AdvVoiceSteal     => T("When Limit Reached", "한도 도달 시");
        public static string AdvStealSkip      => T("Skip new sound", "새 소리 무시");
        public static string AdvStealOldest    => T("Stop the oldest", "가장 오래된 소리 끊기");
        public static string AdvStealQuietest  => T("Stop the quietest", "가장 작은 소리 끊기");
        public static string AdvVoicesHint     => T(
            "Limits how many copies of this sound play at once, preventing noisy overlap and saving performance.",
            "이 사운드가 동시에 몇 개까지 재생될지 제한합니다. 소리가 겹쳐 시끄러워지는 것과 성능 낭비를 막습니다.");

        // 재생 지연
        public static string AdvDelay        => T("Play Delay (s)", "재생 지연 (초)");
        public static string AdvDelayTitle   => T("Play Delay", "재생 지연");
        public static string AdvDelayHint    => T(
            "Waits this many seconds after the play request before the sound is heard.",
            "재생을 요청한 뒤 이 시간(초)만큼 기다렸다가 소리가 납니다.");

        // 동작
        public static string AdvSectionBehavior => T("Behavior", "동작");
        public static string AdvIgnorePause     => T("Play While Game Paused", "일시정지 중에도 재생");
        public static string AdvIgnorePauseHint => T(
            "Keeps playing even when audio is paused (AudioListener.pause). Useful for UI/menu sounds.",
            "오디오 일시정지(AudioListener.pause) 중에도 계속 재생됩니다. UI/메뉴 사운드에 유용합니다.");
        public static string AdvPersist         => T("Keep Playing Across Scenes", "씬 전환에도 유지");
        public static string AdvPersistHint     => T(
            "Won't stop on scene change. Note: the SoundManager itself must survive the scene load (DontDestroyOnLoad).",
            "씬이 바뀌어도 멈추지 않습니다. 단, SoundManager 오브젝트가 씬 전환 후에도 살아있어야 합니다(DontDestroyOnLoad).");

        // 랜덤 음량
        public static string AdvSectionVolume => T("Random Volume", "랜덤 음량");
        public static string AdvRandomVolume  => T("Random Volume", "랜덤 음량");
        public static string AdvVolumeHint    => T(
            "Plays at a random volume between Min and Max each time. Adds natural variation.",
            "재생할 때마다 Min~Max 사이의 무작위 음량으로 재생됩니다. 자연스러운 변화를 줍니다.");

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
