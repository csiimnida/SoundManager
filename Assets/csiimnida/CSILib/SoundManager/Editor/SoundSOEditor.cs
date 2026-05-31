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
        private StyleSheet themeStyle;

        private Label nameLabel;
        private Label typeTag;
        private TextField nameField;
        private EnumField typeField;

        private Button playButton, pushButton, stopButton;
        private Slider playSlider;
        private ObjectField audioClipField;

        private Toggle randPitchToggle;
        private FloatField minPitchField, maxPitchField;
        private Slider pitchSlider, spatialBlendSlider;
        private VisualElement section3D;

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
            if (themeStyle != null) root.styleSheets.Add(themeStyle);
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
            root.Q<FloatField>("MinValue").label        = L10n.LabelMin;
            root.Q<FloatField>("MaxValue").label        = L10n.LabelMax;
            root.Q<FloatField>("FadeInField").label     = L10n.LabelFadeIn;
            root.Q<FloatField>("FadeOutField").label    = L10n.LabelFadeOut;
            root.Q<Label>("FadeHint").text              = L10n.HintFade;
            root.Q<FloatField>("CooldownField").label   = L10n.LabelCooldown;
            root.Q<Label>("CooldownHint").text          = L10n.HintCooldown;

            root.Q<Button>("PlayBt").text  = L10n.Play;
            root.Q<Button>("PushBt").text  = L10n.Pause;
            root.Q<Button>("StopBt").text  = L10n.Stop;
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
            minPitchField      = root.Q<FloatField>("MinValue");
            maxPitchField      = root.Q<FloatField>("MaxValue");
            pitchSlider        = root.Q<Slider>("PitchField");
            spatialBlendSlider = root.Q<Slider>("SpatialBlendField");
            section3D          = root.Q<VisualElement>("Section3D");
            playSlider         = root.Q<Slider>("PlayBar");
            playButton         = root.Q<Button>("PlayBt");
        }

        // ── 이벤트 등록 ─────────────────────────────────────────────

        private void SetEvents(VisualElement root)
        {
            playButton.clicked += HandlePlayButton;
            nameField.RegisterValueChangedCallback(HandleAssetNameChange);
            audioClipField.RegisterValueChangedCallback(HandleAudioChange);
            randPitchToggle.RegisterValueChangedCallback(HandleChangeRandPitchValue);
            minPitchField.RegisterValueChangedCallback(HandleChangeMinValue);
            maxPitchField.RegisterValueChangedCallback(HandleChangeMaxValue);
            typeField.RegisterValueChangedCallback(evt => SetTypeTag((SoundType)evt.newValue));
            spatialBlendSlider.RegisterValueChangedCallback(evt => Set3DSectionVisible(evt.newValue > 0f));

            pushButton = root.Q<Button>("PushBt");
            pushButton.clicked += HandlePauseButton;
            pushButton.SetEnabled(false);

            stopButton = root.Q<Button>("StopBt");
            stopButton.clicked += () => { EditorAudioUtil.Stop(); EndSound(); };
            stopButton.SetEnabled(false);

            minPitchField.value = soundcs.MinPitch;
            maxPitchField.value = soundcs.MaxPitch;

            bool randOn = soundcs.RandomPitch;
            pitchSlider.SetEnabled(!randOn);
            minPitchField.SetEnabled(randOn);
            maxPitchField.SetEnabled(randOn);
        }

        // ── 타입 태그 / 3D 섹션 ─────────────────────────────────────

        private void RefreshTypeTag() => SetTypeTag(soundcs.soundType);

        private void SetTypeTag(SoundType type)
        {
            if (typeTag == null) return;
            bool isBgm = type == SoundType.BGM;
            typeTag.text = isBgm ? "BGM" : "SFX";
            typeTag.EnableInClassList("sm-tag--bgm", isBgm);
            typeTag.EnableInClassList("sm-tag--sfx", !isBgm);
        }

        private void Refresh3DSection() => Set3DSectionVisible(soundcs.SpatialBlend > 0f);

        private void Set3DSectionVisible(bool show)
        {
            if (section3D == null) return;
            section3D.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── 재생 제어 (EditorAudioUtil) ─────────────────────────────

        private void HandlePlayButton()
        {
            if (soundcs.clip == null)
            {
                EditorUtility.DisplayDialog(L10n.AudioClipNullTitle, L10n.AudioClipNullMessage, L10n.Ok);
                return;
            }

            EditorAudioUtil.Stop();
            _previewClip = soundcs.clip;
            EditorAudioUtil.Play(_previewClip, loop: soundcs.loop);

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
                EditorAudioUtil.Pause();
                _isPlaying = false;
                _isPaused  = true;
                pushButton.text = L10n.Resume;
            }
            else
            {
                EditorAudioUtil.Resume();
                _isPlaying = true;
                _isPaused  = false;
                pushButton.text = L10n.Pause;
            }
        }

        private void Update()
        {
            if (!_isPlaying || _previewClip == null) return;

            if (!EditorAudioUtil.IsPlaying() && !soundcs.loop)
            {
                EndSound();
                return;
            }

            float len = _previewClip.length;
            if (len > 0f && playSlider != null)
                playSlider.value = Mathf.Clamp01(EditorAudioUtil.GetPosition() / len);
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
            EditorAudioUtil.Stop();
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
            minPitchField.SetEnabled(evt.newValue);
            maxPitchField.SetEnabled(evt.newValue);
            soundcs.RandomPitch = evt.newValue;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
        }

        private void HandleChangeMinValue(ChangeEvent<float> evt)
        {
            if (!soundcs.RandomPitch) return;

            if (evt.newValue < -3f)
            {
                ShowWarning(L10n.PitchMinBelowNeg3);
                (evt.target as FloatField)?.SetValueWithoutNotify(-3f);
                return;
            }
            if (evt.newValue > soundcs.MaxPitch)
            {
                ShowWarning(L10n.PitchMinAboveMax);
                (evt.target as FloatField)?.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            soundcs.MinPitch = evt.newValue;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
        }

        private void HandleChangeMaxValue(ChangeEvent<float> evt)
        {
            if (!soundcs.RandomPitch) return;

            if (evt.newValue > 3f)
            {
                ShowWarning(L10n.PitchMaxAbove3);
                (evt.target as FloatField)?.SetValueWithoutNotify(3f);
                return;
            }
            if (evt.newValue < soundcs.MinPitch)
            {
                ShowWarning(L10n.PitchMaxBelowMin);
                (evt.target as FloatField)?.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            soundcs.MaxPitch = evt.newValue;
            EditorUtility.SetDirty(soundcs);
            AssetDatabase.SaveAssets();
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

            string themePath = $"{_rootFolderPath}/Editor/UIS/SoundManagerTheme.uss";
            themeStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(themePath);
        }
    }
}
