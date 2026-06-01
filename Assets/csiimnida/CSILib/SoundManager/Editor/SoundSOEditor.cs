using System.IO;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace csiimnida.CSILib.SoundManager.Editor
{
    using L10n = EditorLocalization;

    [UnityEditor.CustomEditor(typeof(SoundSo))]
    public class SoundSOEditor : UnityEditor.Editor
    {
        private VisualTreeAsset visualTreeAsset;

        private Label nameLabel;
        private Label typeTag;
        private TextField nameField;
        private EnumField typeField;

        private Button playButton, pushButton, stopButton;
        private Slider playSlider;
        private ObjectField audioClipField;

        private Toggle randPitchToggle;
        private MinMaxSlider pitchRangeSlider;
        private FloatField pitchMinInput;
        private FloatField pitchMaxInput;
        private VisualElement pitchCenterMarker;
        private Label pitchRangeValueLabel;
        private Slider pitchSlider, spatialBlendSlider, volumeSlider;
        private VisualElement section3D;
        private bool _suppressPitchRangeEvents;

        private bool _isPlaying;
        private bool _isPaused;
        private AudioClip _previewClip;

        private SoundSo soundcs;
        private string _rootFolderPath;

        // ── 진입점 ──────────────────────────────────────────────────

        public override VisualElement CreateInspectorGUI()
        {
            L10n.Reload();
            InitializeWindow();

            VisualElement root = new VisualElement();
            visualTreeAsset.CloneTree(root);

            SetValue(root);
            ApplyUxmlTranslations(root);
            SetEvents(root);
            RefreshTypeTag();
            Refresh3DSection();

            return root;
        }

        // ── UXML 번역 적용 ──────────────────────────────────────────

        private void ApplyUxmlTranslations(VisualElement root)
        {
            // 섹션 제목
            root.Q<Label>("SectionBasicTitle").text    = L10n.SectionBasic;
            root.Q<Label>("SectionAudioTitle").text     = L10n.SectionAudio;
            root.Q<Label>("Section3DTitle").text        = L10n.Section3D;
            root.Q<Label>("SectionRandomTitle").text    = L10n.SectionRandom;
            root.Q<Label>("SectionFadeTitle").text      = L10n.SectionFade;
            root.Q<Label>("SectionCooldownTitle").text  = L10n.SectionCooldown;
            root.Q<Label>("PreviewLabel").text          = L10n.SectionPreview;

            // 필드 레이블
            root.Q<TextField>("SoundNameField").label   = L10n.LabelSoundName;
            root.Q<EnumField>("SoundTypeField").label   = L10n.LabelSoundType;
            root.Q<ObjectField>("AudioField").label     = L10n.LabelAudio;
            root.Q<Toggle>("LoopField").label           = L10n.LabelIsLoop;
            root.Q<SliderInt>("PriorityField").label    = L10n.LabelPriority;
            root.Q<Label>("PriorityHint").text          = L10n.HintPriority;
            root.Q<Slider>("VolumeField").label         = L10n.LabelVolume;
            root.Q<Slider>("PitchField").label          = L10n.LabelPitch;
            root.Q<Slider>("SoundPosField").label       = L10n.LabelStereoPan;
            root.Q<Label>("StereoPanHint").text         = L10n.HintStereoPan;
            root.Q<Slider>("SpatialBlendField").label   = L10n.LabelSpatialBlend;
            root.Q<Label>("SpatialBlendHint").text      = L10n.HintSpatialBlend;
            root.Q<FloatField>("MinDistanceField").label= L10n.LabelMinDistance;
            root.Q<FloatField>("MaxDistanceField").label= L10n.LabelMaxDistance;
            root.Q<Label>("Hint3D").text                = L10n.Hint3D;
            root.Q<Toggle>("RendomPitchBt").label       = L10n.LabelRandomPitch;
            root.Q<FloatField>("PitchMinInput").label   = L10n.LabelMin;
            root.Q<FloatField>("PitchMaxInput").label   = L10n.LabelMax;
            root.Q<FloatField>("FadeInField").label     = L10n.LabelFadeIn;
            root.Q<FloatField>("FadeOutField").label    = L10n.LabelFadeOut;
            root.Q<Label>("FadeHint").text              = L10n.HintFade;
            root.Q<FloatField>("CooldownField").label   = L10n.LabelCooldown;
            root.Q<Label>("CooldownHint").text          = L10n.HintCooldown;

            root.Q<Button>("PlayBt").text  = L10n.Play;
            root.Q<Button>("PushBt").text  = L10n.Pause;
            root.Q<Button>("StopBt").text  = L10n.Stop;

            var advBtn = root.Q<Button>("AdvancedSettingsBt");
            if (advBtn != null) advBtn.text = "\u2699 " + L10n.AdvancedBtn;
        }

        // ── 값 초기화 ───────────────────────────────────────────────

        private void SetValue(VisualElement root)
        {
            soundcs = (SoundSo)target;
            EditorApplication.update += Update;

            nameField = root.Q<TextField>("SoundNameField");
            nameField.value = soundcs.name;

            nameLabel = root.Q<Label>("NameLabel");
            nameLabel.text = soundcs.name;

            typeTag  = root.Q<Label>("TypeTag");
            typeField = root.Q<EnumField>("SoundTypeField");

            audioClipField = root.Q<ObjectField>("AudioField");
            audioClipField.value = soundcs.clip;

            randPitchToggle    = root.Q<Toggle>("RendomPitchBt");
            pitchRangeSlider   = root.Q<MinMaxSlider>("PitchRangeSlider");
            pitchMinInput      = root.Q<FloatField>("PitchMinInput");
            pitchMaxInput      = root.Q<FloatField>("PitchMaxInput");
            pitchCenterMarker  = root.Q<VisualElement>("PitchCenterMarker");
            pitchRangeValueLabel = root.Q<Label>("PitchRangeValueLabel");
            pitchSlider        = root.Q<Slider>("PitchField");
            spatialBlendSlider = root.Q<Slider>("SpatialBlendField");
            volumeSlider       = root.Q<Slider>("VolumeField");
            if (volumeSlider != null)
                volumeSlider.SetEnabled(!soundcs.RandomVolume);
            section3D          = root.Q<VisualElement>("Section3D");
            playSlider         = root.Q<Slider>("PlayBar");
            playButton         = root.Q<Button>("PlayBt");

            if (pitchRangeSlider != null)
            {
                pitchRangeSlider.lowLimit = -3f;
                pitchRangeSlider.highLimit = 3f;
                pitchRangeSlider.SetValueWithoutNotify(new Vector2(soundcs.MinPitch, soundcs.MaxPitch));
            }
            if (pitchCenterMarker != null)
            {
                pitchCenterMarker.style.left = Length.Percent(GetCenterMarkerPercent());
                pitchCenterMarker.pickingMode = PickingMode.Ignore;
            }

            ApplyPitchRange(soundcs.MinPitch, soundcs.MaxPitch, save: false);
        }

        // ── 이벤트 등록 ─────────────────────────────────────────────

        private void SetEvents(VisualElement root)
        {
            playButton.clicked += HandlePlayButton;
            nameField.RegisterValueChangedCallback(HandleAssetNameChange);
            audioClipField.RegisterValueChangedCallback(HandleAudioChange);
            randPitchToggle.RegisterValueChangedCallback(HandleChangeRandPitchValue);
            pitchRangeSlider?.RegisterValueChangedCallback(HandleChangePitchRange);
            pitchMinInput?.RegisterValueChangedCallback(HandleChangePitchMinInput);
            pitchMaxInput?.RegisterValueChangedCallback(HandleChangePitchMaxInput);
            typeField.RegisterValueChangedCallback(evt => SetTypeTag((SoundType)evt.newValue));
            spatialBlendSlider.RegisterValueChangedCallback(evt => Set3DSectionVisible(evt.newValue > 0f));

            pushButton = root.Q<Button>("PushBt");
            pushButton.clicked += HandlePauseButton;
            pushButton.SetEnabled(false);

            stopButton = root.Q<Button>("StopBt");
            stopButton.clicked += () => { SoundPreviewPlayer.Stop(); EndSound(); };
            stopButton.SetEnabled(false);

            var advBtn = root.Q<Button>("AdvancedSettingsBt");
            if (advBtn != null) advBtn.clicked += () => SoundAdvancedSettingsWindow.Open(soundcs);

            bool randOn = soundcs.RandomPitch;
            pitchSlider.SetEnabled(!randOn);
            if (pitchRangeSlider != null) pitchRangeSlider.SetEnabled(randOn);
            if (pitchMinInput != null) pitchMinInput.SetEnabled(randOn);
            if (pitchMaxInput != null) pitchMaxInput.SetEnabled(randOn);
        }

        // ── 타입 태그 / 3D 섹션 ─────────────────────────────────────

        private void RefreshTypeTag() => SetTypeTag(soundcs.soundType);

        private void SetTypeTag(SoundType type)
        {
            if (typeTag == null) return;
            bool isBgm = type == SoundType.BGM;
            typeTag.text = isBgm ? "BGM" : "SFX";
            typeTag.style.backgroundColor = isBgm
                ? new Color(0.72f, 0.58f, 0.96f, 0.35f)
                : new Color(0.96f, 0.77f, 0.33f, 0.35f);
        }

        private void Refresh3DSection() => Set3DSectionVisible(soundcs.SpatialBlend > 0f);

        private void Set3DSectionVisible(bool show)
        {
            if (section3D == null) return;
            section3D.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── 재생 제어 (SoundPreviewPlayer) ──────────────────────────

        private void HandlePlayButton()
        {
            if (soundcs.clip == null)
            {
                EditorUtility.DisplayDialog(L10n.AudioClipNullTitle, L10n.AudioClipNullMessage, L10n.Ok);
                return;
            }

            SoundPreviewPlayer.Stop();
            _previewClip = soundcs.clip;
            SoundPreviewPlayer.Play(soundcs);

            _isPlaying = true;
            _isPaused  = false;
            pushButton.SetEnabled(true);
            stopButton.SetEnabled(true);
            pushButton.text = L10n.Pause;
        }

        private void HandlePauseButton()
        {
            if (!_isPlaying && !_isPaused) return;

            if (_isPlaying)
            {
                SoundPreviewPlayer.Pause();
                _isPlaying = false;
                _isPaused  = true;
                pushButton.text = L10n.Resume;
            }
            else
            {
                SoundPreviewPlayer.Resume();
                _isPlaying = true;
                _isPaused  = false;
                pushButton.text = L10n.Pause;
            }
        }

        private void Update()
        {
            // 랜덤 음량이 켜져 있으면 일반 음량 슬라이더를 비활성화 (고급 설정 창에서 바뀌어도 즉시 반영)
            if (volumeSlider != null && soundcs != null)
                volumeSlider.SetEnabled(!soundcs.RandomVolume);

            if (!_isPlaying || _previewClip == null) return;

            if (!SoundPreviewPlayer.IsPlaying() && !soundcs.loop)
            {
                EndSound();
                return;
            }

            float len = SoundPreviewPlayer.CurrentClipLength;
            if (len > 0f && playSlider != null)
                playSlider.value = Mathf.Clamp01(SoundPreviewPlayer.GetPosition() / len);
        }

        private void EndSound()
        {
            _isPlaying   = false;
            _isPaused    = false;
            _previewClip = null;
            if (pushButton != null) { pushButton.text = L10n.Pause; pushButton.SetEnabled(false); }
            if (stopButton != null) stopButton.SetEnabled(false);
            if (playSlider != null) playSlider.value = 0f;
        }

        public void ResetSound()
        {
            EditorApplication.update -= Update;
            SoundPreviewPlayer.Stop();
            _isPlaying = false;
            _isPaused  = false;
        }

        private void OnDestroy() => ResetSound();

        // ── 필드 이벤트 핸들러 ──────────────────────────────────────

        private void HandleAudioChange(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                EditorUtility.DisplayDialog(L10n.AudioClipNullTitle, L10n.AudioClipNullMessage, L10n.Ok);
                (evt.target as ObjectField)?.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            soundcs.clip = evt.newValue as AudioClip;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
        }

        private void HandleChangeRandPitchValue(ChangeEvent<bool> evt)
        {
            pitchSlider.SetEnabled(!evt.newValue);
            if (pitchRangeSlider != null) pitchRangeSlider.SetEnabled(evt.newValue);
            if (pitchMinInput != null) pitchMinInput.SetEnabled(evt.newValue);
            if (pitchMaxInput != null) pitchMaxInput.SetEnabled(evt.newValue);
            soundcs.RandomPitch = evt.newValue;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
        }

        private void HandleChangePitchRange(ChangeEvent<Vector2> evt)
        {
            if (_suppressPitchRangeEvents || !soundcs.RandomPitch) return;
            ApplyPitchRange(evt.newValue.x, evt.newValue.y, save: true);
        }

        private void HandleChangePitchMinInput(ChangeEvent<float> evt)
        {
            if (_suppressPitchRangeEvents || !soundcs.RandomPitch) return;
            ApplyPitchRange(evt.newValue, soundcs.MaxPitch, save: true);
        }

        private void HandleChangePitchMaxInput(ChangeEvent<float> evt)
        {
            if (_suppressPitchRangeEvents || !soundcs.RandomPitch) return;
            ApplyPitchRange(soundcs.MinPitch, evt.newValue, save: true);
        }

        private void ApplyPitchRange(float min, float max, bool save)
        {
            min = Mathf.Clamp(min, -3f, 3f);
            max = Mathf.Clamp(max, -3f, 3f);
            if (min > max) (min, max) = (max, min);

            _suppressPitchRangeEvents = true;
            if (pitchRangeSlider != null)
                pitchRangeSlider.SetValueWithoutNotify(new Vector2(min, max));
            pitchMinInput?.SetValueWithoutNotify(min);
            pitchMaxInput?.SetValueWithoutNotify(max);
            _suppressPitchRangeEvents = false;

            soundcs.MinPitch = min;
            soundcs.MaxPitch = max;
            UpdatePitchRangeLabel(min, max);

            if (!save) return;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
        }

        private static float GetCenterMarkerPercent()
        {
            const float low = -3f;
            const float high = 3f;
            const float centerValue = 1f;
            return Mathf.Clamp01((centerValue - low) / (high - low)) * 100f;
        }

        private void UpdatePitchRangeLabel(float min, float max)
        {
            if (pitchRangeValueLabel == null) return;
            pitchRangeValueLabel.text = $"{L10n.LabelMin} {min:0.00} / {L10n.LabelMax} {max:0.00}";
        }

        private void HandleAssetNameChange(ChangeEvent<string> evt)
        {
            if (string.IsNullOrEmpty(evt.newValue))
            {
                ShowWarning(L10n.NameEmptyMessage);
                (evt.target as TextField)?.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            string assetPath = AssetDatabase.GetAssetPath(target);
            string message = AssetDatabase.RenameAsset(assetPath, evt.newValue);
            if (string.IsNullOrEmpty(message))
            {
                target.name = evt.newValue;
                soundcs.soundName = evt.newValue;
                nameLabel.text = evt.newValue;
            }
            else
            {
                (evt.target as TextField)?.SetValueWithoutNotify(evt.previousValue);
                EditorUtility.DisplayDialog("Error", message, L10n.Ok);
            }
        }

        private void ShowWarning(string msg)
            => EditorUtility.DisplayDialog(L10n.Warning, msg, L10n.Ok);

        // ── 초기화 ──────────────────────────────────────────────────

        private void InitializeWindow()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            _rootFolderPath = Directory.GetParent(Path.GetDirectoryName(scriptPath)).FullName.Replace("\\", "/");
            _rootFolderPath = "Assets" + _rootFolderPath.Substring(Application.dataPath.Length);

            // 언어와 무관하게 단일 UXML 사용, 번역은 ApplyUxmlTranslations 에서 적용
            string uxmlPath = $"{_rootFolderPath}/Editor/UIS/SoundSO_en_vr.uxml";
            visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            Debug.Assert(visualTreeAsset != null, $"Visual tree asset is null: {uxmlPath}");
        }
    }
}
