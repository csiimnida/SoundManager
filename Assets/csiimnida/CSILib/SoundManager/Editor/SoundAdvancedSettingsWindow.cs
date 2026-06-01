using System.Collections.Generic;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace csiimnida.CSILib.SoundManager.Editor
{
    using L10n = EditorLocalization;

    /// <summary>
    /// 사운드의 고급 설정을 시각적으로 편집하는 창.
    /// 초보자가 한 번 보고 이해할 수 있도록 섹션별 카드 + 한 줄 설명 + 파형 시각화로 구성했습니다.
    /// </summary>
    public class SoundAdvancedSettingsWindow : EditorWindow
    {
        private SoundSo _so;

        // 파형 / 시작 지점
        private VisualElement _waveform;
        private VisualElement _skipRegion;
        private VisualElement _startMarker;
        private VisualElement _playhead;
        private Texture2D _waveTex;
        private FloatField _startField;
        private Label _startReadout;

        // 미리듣기 상태
        private bool _isPlaying;
        private AudioClip _previewClip;

        private const float WaveHeight = 96f;

        // 섹션별 강조색
        private static readonly Color CStart    = new Color(0.36f, 0.72f, 0.98f);
        private static readonly Color CVariation = new Color(0.55f, 0.85f, 0.5f);
        private static readonly Color CVoices   = new Color(0.95f, 0.7f, 0.35f);
        private static readonly Color CDelay    = new Color(0.45f, 0.8f, 0.85f);
        private static readonly Color CBehavior = new Color(0.9f, 0.55f, 0.55f);
        private static readonly Color CVolume   = new Color(0.8f, 0.55f, 0.9f);

        public static void Open(SoundSo so)
        {
            if (so == null) return;
            var win = GetWindow<SoundAdvancedSettingsWindow>(true, L10n.AdvancedTitle, true);
            win.minSize = new Vector2(440, 560);
            win.Init(so);
            win.Show();
        }

        private void Init(SoundSo so)
        {
            _so = so;
            titleContent = new GUIContent(L10n.AdvancedTitle);
            RebuildUI();
        }

        private void OnEnable()  => EditorApplication.update += OnEditorUpdate;
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SoundPreviewPlayer.Stop();
            DestroyWaveTexture();
        }

        private void DestroyWaveTexture()
        {
            if (_waveTex != null)
            {
                DestroyImmediate(_waveTex);
                _waveTex = null;
            }
        }

        // ════════════════════════════════════════════════════════════
        // UI 구성
        // ════════════════════════════════════════════════════════════
        private void RebuildUI()
        {
            L10n.Reload();
            rootVisualElement.Clear();

            if (_so == null)
            {
                rootVisualElement.Add(new HelpBox("No sound selected.", HelpBoxMessageType.Info));
                return;
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.style.flexGrow = 1;
            rootVisualElement.Add(scroll);

            scroll.Add(new Label(_so.name)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, marginTop = 6, marginLeft = 8, marginBottom = 2 },
            });
            scroll.Add(new Label(L10n.AdvancedTitle)
            {
                style = { color = new Color(0.66f, 0.66f, 0.66f), marginLeft = 8, marginBottom = 8 },
            });

            scroll.Add(BuildStartSection());      // A-시작지점/랜덤시작(②)
            scroll.Add(BuildVariationSection());  // A-①
            scroll.Add(BuildVoicesSection());     // B-③④
            scroll.Add(BuildDelaySection());      // C-⑤
            scroll.Add(BuildBehaviorSection());   // D-⑧⑨
            scroll.Add(BuildRandomVolumeSection());

            var footer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 10, marginLeft = 8, marginRight = 8, marginBottom = 10 },
            };
            var resetBtn = new Button(ResetToDefault) { text = L10n.AdvResetBtn };
            resetBtn.style.flexGrow = 1; resetBtn.style.height = 26; resetBtn.style.marginRight = 4;
            var closeBtn = new Button(Close) { text = L10n.AdvCloseBtn };
            closeBtn.style.flexGrow = 1; closeBtn.style.height = 26;
            footer.Add(resetBtn);
            footer.Add(closeBtn);
            scroll.Add(footer);

            RefreshStartVisual();
        }

        // ── 공통 UI 헬퍼 ─────────────────────────────────────────────
        private static VisualElement Card(string title, Color accent)
        {
            var card = new VisualElement
            {
                style =
                {
                    marginLeft = 8, marginRight = 8, marginBottom = 10,
                    paddingTop = 8, paddingBottom = 10, paddingLeft = 10, paddingRight = 10,
                    borderTopLeftRadius = 6, borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6, borderBottomRightRadius = 6,
                    backgroundColor = new Color(0.20f, 0.20f, 0.20f, 0.55f),
                    borderLeftWidth = 3, borderLeftColor = accent,
                },
            };
            card.Add(new Label(title)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, marginBottom = 6 },
            });
            return card;
        }

        private static Label Hint(string text)
        {
            return new Label(text)
            {
                style = { fontSize = 11, whiteSpace = WhiteSpace.Normal, color = new Color(0.62f, 0.62f, 0.62f), marginTop = 4 },
            };
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 재생 시작 지점 (파형 + 드래그 마커)
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildStartSection()
        {
            var card = Card(L10n.AdvSectionStart, CStart);

            _waveform = new VisualElement
            {
                style =
                {
                    height = WaveHeight, marginBottom = 6,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                    overflow = Overflow.Hidden,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f),
                },
            };

            _skipRegion = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, top = 0, bottom = 0, width = 0, backgroundColor = new Color(0f, 0f, 0f, 0.55f) },
            };
            _startMarker = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, top = 0, bottom = 0, width = 2, left = 0, backgroundColor = CStart },
            };
            _playhead = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, top = 0, bottom = 0, width = 1, left = 0, backgroundColor = new Color(1f, 0.85f, 0.2f, 0.9f), display = DisplayStyle.None },
            };
            _waveform.Add(_skipRegion);
            _waveform.Add(_startMarker);
            _waveform.Add(_playhead);

            _waveform.RegisterCallback<PointerDownEvent>(OnWavePointerDown);
            _waveform.RegisterCallback<PointerMoveEvent>(OnWavePointerMove);
            _waveform.RegisterCallback<PointerUpEvent>(OnWavePointerUp);
            _waveform.RegisterCallback<GeometryChangedEvent>(_ => RedrawWaveform());
            card.Add(_waveform);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            _startField = new FloatField(L10n.AdvStartOffset) { value = _so.startOffset };
            _startField.style.flexGrow = 1;
            _startField.RegisterValueChangedCallback(evt => SetStartOffset(evt.newValue, fromField: true));
            row.Add(_startField);
            _startReadout = new Label { style = { marginLeft = 8, minWidth = 90, color = new Color(0.7f, 0.7f, 0.7f) } };
            row.Add(_startReadout);
            card.Add(row);

            var randToggle = new Toggle(L10n.AdvRandomStart) { value = _so.randomStartPosition };
            randToggle.RegisterValueChangedCallback(e => { _so.randomStartPosition = e.newValue; Save(); });
            card.Add(randToggle);

            var playBtn = new Button(PlayFromStart) { text = "\u25B6 " + L10n.AdvPlayFromStart };
            playBtn.style.marginTop = 6; playBtn.style.height = 24;
            card.Add(playBtn);

            card.Add(Hint(L10n.AdvStartHint));
            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 클립 배리에이션 (A-①)
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildVariationSection()
        {
            var card = Card(L10n.AdvSectionVariation, CVariation);

            // 기본 클립을 회색으로 안내 (목록에 자동 포함됨)
            if (_so.clip != null)
                card.Add(new Label($"\u2605 {_so.clip.name}  (Main)") { style = { fontSize = 11, color = new Color(0.7f, 0.7f, 0.7f), marginBottom = 4 } });

            _so.clipVariations ??= new List<AudioClip>();

            var listContainer = new VisualElement();
            card.Add(listContainer);

            void RebuildList()
            {
                listContainer.Clear();
                for (int i = 0; i < _so.clipVariations.Count; i++)
                {
                    int index = i;
                    var rowEl = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                    var field = new ObjectField { objectType = typeof(AudioClip), allowSceneObjects = false, value = _so.clipVariations[index] };
                    field.style.flexGrow = 1;
                    field.RegisterValueChangedCallback(e => { _so.clipVariations[index] = e.newValue as AudioClip; Save(); });
                    rowEl.Add(field);

                    var del = new Button(() => { _so.clipVariations.RemoveAt(index); Save(); RebuildList(); }) { text = "\u2715" };
                    del.style.width = 24; del.style.marginLeft = 4;
                    rowEl.Add(del);
                    listContainer.Add(rowEl);
                }
            }
            RebuildList();

            var addBtn = new Button(() => { _so.clipVariations.Add(null); Save(); RebuildList(); }) { text = L10n.AdvAddClip };
            addBtn.style.marginTop = 4;
            card.Add(addBtn);

            card.Add(Hint(L10n.AdvVariationHint));
            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 동시 재생 (B-③④)
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildVoicesSection()
        {
            var card = Card(L10n.AdvSectionVoices, CVoices);

            var maxField = new IntegerField(L10n.AdvMaxVoices) { value = Mathf.Max(0, _so.maxVoices) };

            var stealDropdown = new DropdownField(L10n.AdvVoiceSteal,
                new List<string> { L10n.AdvStealSkip, L10n.AdvStealOldest, L10n.AdvStealQuietest },
                Mathf.Clamp((int)_so.voiceSteal, 0, 2));

            void RefreshStealEnabled() => stealDropdown.SetEnabled(_so.maxVoices > 0);

            maxField.RegisterValueChangedCallback(e =>
            {
                _so.maxVoices = Mathf.Max(0, e.newValue);
                if (_so.maxVoices != e.newValue) maxField.SetValueWithoutNotify(_so.maxVoices);
                RefreshStealEnabled();
                Save();
            });
            stealDropdown.RegisterValueChangedCallback(e =>
            {
                _so.voiceSteal = (VoiceStealMode)Mathf.Clamp(stealDropdown.index, 0, 2);
                Save();
            });

            card.Add(maxField);
            card.Add(stealDropdown);
            RefreshStealEnabled();

            card.Add(Hint(L10n.AdvVoicesHint));
            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 재생 지연 (C-⑤)
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildDelaySection()
        {
            var card = Card(L10n.AdvDelayTitle, CDelay);
            var field = new FloatField(L10n.AdvDelay) { value = Mathf.Max(0f, _so.playDelay) };
            field.RegisterValueChangedCallback(e =>
            {
                _so.playDelay = Mathf.Max(0f, e.newValue);
                if (_so.playDelay != e.newValue) field.SetValueWithoutNotify(_so.playDelay);
                Save();
            });
            card.Add(field);
            card.Add(Hint(L10n.AdvDelayHint));
            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 동작 (D-⑧⑨)
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildBehaviorSection()
        {
            var card = Card(L10n.AdvSectionBehavior, CBehavior);

            var pauseToggle = new Toggle(L10n.AdvIgnorePause) { value = _so.ignoreListenerPause };
            pauseToggle.RegisterValueChangedCallback(e => { _so.ignoreListenerPause = e.newValue; Save(); });
            card.Add(pauseToggle);
            card.Add(Hint(L10n.AdvIgnorePauseHint));

            var persistToggle = new Toggle(L10n.AdvPersist) { value = _so.persistAcrossScenes };
            persistToggle.RegisterValueChangedCallback(e => { _so.persistAcrossScenes = e.newValue; Save(); });
            card.Add(persistToggle);
            card.Add(Hint(L10n.AdvPersistHint));

            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 섹션: 랜덤 음량
        // ════════════════════════════════════════════════════════════
        private VisualElement BuildRandomVolumeSection()
        {
            var card = Card(L10n.AdvSectionVolume, CVolume);

            var toggle = new Toggle(L10n.AdvRandomVolume) { value = _so.RandomVolume };
            card.Add(toggle);

            var rangeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 } };
            var minField = new FloatField(L10n.LabelMin) { value = _so.MinVolume };
            minField.style.width = 110; minField.style.marginRight = 6;
            var slider = new MinMaxSlider(_so.MinVolume, _so.MaxVolume, 0f, 1f);
            slider.style.flexGrow = 1;
            var maxField = new FloatField(L10n.LabelMax) { value = _so.MaxVolume };
            maxField.style.width = 110; maxField.style.marginLeft = 6;
            rangeRow.Add(minField);
            rangeRow.Add(slider);
            rangeRow.Add(maxField);
            card.Add(rangeRow);

            bool suppress = false;
            void Apply(float min, float max)
            {
                min = Mathf.Clamp01(min);
                max = Mathf.Clamp01(max);
                if (min > max) (min, max) = (max, min);
                suppress = true;
                slider.SetValueWithoutNotify(new Vector2(min, max));
                minField.SetValueWithoutNotify(min);
                maxField.SetValueWithoutNotify(max);
                suppress = false;
                _so.MinVolume = min; _so.MaxVolume = max;
                Save();
            }
            slider.RegisterValueChangedCallback(e => { if (!suppress) Apply(e.newValue.x, e.newValue.y); });
            minField.RegisterValueChangedCallback(e => { if (!suppress) Apply(e.newValue, _so.MaxVolume); });
            maxField.RegisterValueChangedCallback(e => { if (!suppress) Apply(_so.MinVolume, e.newValue); });

            rangeRow.SetEnabled(_so.RandomVolume);
            toggle.RegisterValueChangedCallback(e => { _so.RandomVolume = e.newValue; rangeRow.SetEnabled(e.newValue); Save(); });

            card.Add(Hint(L10n.AdvVolumeHint));
            return card;
        }

        // ════════════════════════════════════════════════════════════
        // 시작 지점 (파형 상호작용)
        // ════════════════════════════════════════════════════════════
        private AudioClip Clip => _so != null ? _so.clip : null;

        private void OnWavePointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            _waveform.CaptureMouse();
            SetStartFromLocalX(e.localPosition.x);
            e.StopPropagation();
        }

        private void OnWavePointerMove(PointerMoveEvent e)
        {
            if (!_waveform.HasMouseCapture()) return;
            SetStartFromLocalX(e.localPosition.x);
            e.StopPropagation();
        }

        private void OnWavePointerUp(PointerUpEvent e)
        {
            if (_waveform.HasMouseCapture())
                _waveform.ReleaseMouse();
        }

        private void SetStartFromLocalX(float localX)
        {
            float w = _waveform.resolvedStyle.width;
            if (w <= 0f || Clip == null) return;
            float percent = Mathf.Clamp01(localX / w);
            SetStartOffset(percent * Clip.length, fromField: false);
        }

        private void SetStartOffset(float seconds, bool fromField)
        {
            float len = Clip != null ? Clip.length : 0f;
            seconds = len > 0f ? Mathf.Clamp(seconds, 0f, Mathf.Max(0f, len - 0.01f)) : Mathf.Max(0f, seconds);
            _so.startOffset = seconds;
            if (!fromField && _startField != null)
                _startField.SetValueWithoutNotify(seconds);
            RefreshStartVisual();
            Save();
        }

        private void RefreshStartVisual()
        {
            float len = Clip != null ? Clip.length : 0f;
            float percent = len > 0f ? Mathf.Clamp01(_so.startOffset / len) : 0f;
            if (_skipRegion != null) _skipRegion.style.width = Length.Percent(percent * 100f);
            if (_startMarker != null) _startMarker.style.left = Length.Percent(percent * 100f);
            if (_startReadout != null) _startReadout.text = len > 0f ? $"{_so.startOffset:0.00}s / {len:0.00}s" : "\u2014";
        }

        private void RedrawWaveform()
        {
            if (_waveform == null) return;
            float w = _waveform.resolvedStyle.width;
            if (w < 1f || Clip == null) return;

            DestroyWaveTexture();
            _waveTex = SoundWaveformTexture.Build(
                Clip, Mathf.RoundToInt(w), Mathf.RoundToInt(WaveHeight),
                new Color(0.36f, 0.72f, 0.98f, 0.95f), new Color(0.12f, 0.12f, 0.12f, 1f));
            _waveform.style.backgroundImage = new StyleBackground(_waveTex);
            RefreshStartVisual();
        }

        // ════════════════════════════════════════════════════════════
        // 미리듣기
        // ════════════════════════════════════════════════════════════
        private void PlayFromStart()
        {
            if (Clip == null)
            {
                EditorUtility.DisplayDialog(L10n.AudioClipNullTitle, L10n.AudioClipNullMessage, L10n.Ok);
                return;
            }
            SoundPreviewPlayer.Stop();
            _previewClip = Clip;
            SoundPreviewPlayer.Play(_so);
            _isPlaying = true;
            if (_playhead != null) _playhead.style.display = DisplayStyle.Flex;
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying || _previewClip == null || _playhead == null) return;

            if (!SoundPreviewPlayer.IsPlaying() && !(_so != null && _so.loop))
            {
                _isPlaying = false;
                _playhead.style.display = DisplayStyle.None;
                return;
            }

            float len = SoundPreviewPlayer.CurrentClipLength;
            if (len > 0f)
                _playhead.style.left = Length.Percent(Mathf.Clamp01(SoundPreviewPlayer.GetPosition() / len) * 100f);
        }

        // ════════════════════════════════════════════════════════════
        // 저장 / 초기화
        // ════════════════════════════════════════════════════════════
        private void Save()
        {
            if (_so == null) return;
            EditorUtility.SetDirty(_so);
        }

        private void ResetToDefault()
        {
            if (_so == null) return;
            _so.startOffset = 0f;
            _so.randomStartPosition = false;
            _so.clipVariations?.Clear();
            _so.maxVoices = 0;
            _so.voiceSteal = VoiceStealMode.Skip;
            _so.playDelay = 0f;
            _so.ignoreListenerPause = false;
            _so.persistAcrossScenes = false;
            _so.RandomVolume = false;
            _so.MinVolume = 0.9f;
            _so.MaxVolume = 1f;
            Save();
            AssetDatabase.SaveAssets();
            RebuildUI();
        }
    }
}
