using System;
using System.Collections.Generic;
using System.IO;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace csiimnida.CSILib.SoundManager.Editor
{
    using L10n = EditorLocalization;

    public class SoundListEditor : EditorWindow
    {
        // 패키지 내부 파일(UXML/USS) 경로 — 업데이트로 갱신되어도 무관
        private string _packageRootPath;

        private VisualTreeAsset visualTreeAsset;
        private VisualTreeAsset sounditemAsset;
        private StyleSheet themeStyle;

        private UnityEditor.Editor _cachedEditor;
        private SoundListSo soundListSo;

        // 윈도우 요소
        private Button _createBtn, _langBtn, _tabSoundsBtn, _tabSettingsBtn;
        private VisualElement _soundsTab;
        private ScrollView _settingsTab;
        private ScrollView _itemView;
        private ScrollView _inspectorView;

        private List<SoundItemUI> _itemList;
        private SoundItemUI _selectedItem;

        // 설정 탭 요소
        private TextField _dataPathField;
        private Label _toast;

        // ── 메뉴 진입점 ─────────────────────────────────────────────

        [MenuItem("Window/CSILib/SoundManager")]
        public static void ShowWindow()
        {
            SoundListEditor wnd = GetWindow<SoundListEditor>();
            wnd.titleContent = new GUIContent("SoundManager");
            wnd.minSize = new Vector2(560, 420);
        }

        // ── 윈도우 생성 ─────────────────────────────────────────────

        public void CreateGUI()
        {
            L10n.Reload();
            InitializeWindow();

            VisualElement root = rootVisualElement;
            if (themeStyle != null) root.styleSheets.Add(themeStyle);
            visualTreeAsset.CloneTree(root);

            BindElements(root);
            BuildSettingsTab();
            ApplyTranslations();

            LoadSoundListSO();
            GeneratePoolItems();
            ShowTab(true);
        }

        // ── 요소 바인딩 ─────────────────────────────────────────────

        private void BindElements(VisualElement root)
        {
            _createBtn = root.Q<Button>("CreateBtn");
            _createBtn.clicked += HandleCreateBtn;

            _langBtn = root.Q<Button>("LenBtn");
            _langBtn.clicked += HandleLangBtn;

            _tabSoundsBtn   = root.Q<Button>("TabSounds");
            _tabSettingsBtn = root.Q<Button>("TabSettings");
            _tabSoundsBtn.clicked   += () => ShowTab(true);
            _tabSettingsBtn.clicked += () => ShowTab(false);

            _soundsTab     = root.Q<VisualElement>("SoundsTab");
            _settingsTab   = root.Q<ScrollView>("SettingsTab");
            _itemView      = root.Q<ScrollView>("ItemView");
            _inspectorView = root.Q<ScrollView>("InspectorView");

            // 가로 스크롤바가 떴다 사라지며 레이아웃이 흔들리는 것을 방지
            _itemView.mode = ScrollViewMode.Vertical;
            _itemView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _inspectorView.mode = ScrollViewMode.Vertical;
            _inspectorView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _settingsTab.mode = ScrollViewMode.Vertical;
            _settingsTab.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _itemList = new List<SoundItemUI>();
        }

        // ── 번역 적용 ────────────────────────────────────────────────

        private void ApplyTranslations()
        {
            _langBtn.text        = L10n.LangBtn;
            _tabSoundsBtn.text   = L10n.TabSounds;
            _tabSettingsBtn.text = L10n.TabSettings;
        }

        // ── 탭 전환 ──────────────────────────────────────────────────

        private void ShowTab(bool sounds)
        {
            _soundsTab.style.display   = sounds ? DisplayStyle.Flex : DisplayStyle.None;
            _settingsTab.style.display = sounds ? DisplayStyle.None : DisplayStyle.Flex;
            _tabSoundsBtn.EnableInClassList("sm-tab--active", sounds);
            _tabSettingsBtn.EnableInClassList("sm-tab--active", !sounds);
            _createBtn.style.display = sounds ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ════════════════════════════════════════════════════════════
        // Sounds 탭
        // ════════════════════════════════════════════════════════════

        private void HandleCreateBtn()
        {
            SoundManagerConfig.EnsureSoundsFolderExists();

            SoundSo newItem = ScriptableObject.CreateInstance<SoundSo>();
            newItem.soundName = Guid.NewGuid().ToString();

            string assetPath = $"{SoundManagerConfig.SoundsFolder}/{newItem.soundName}.asset";
            AssetDatabase.CreateAsset(newItem, assetPath);

            soundListSo.AddSound(newItem);
            EditorUtility.SetDirty(soundListSo);
            EditorUtility.SetDirty(newItem);
            AssetDatabase.SaveAssets();

            GeneratePoolItems();
        }

        private void HandleLangBtn()
        {
            L10n.Toggle();
            ApplyTranslations();
            RebuildSettingsTab();
            if (_selectedItem?.SoundItem != null) ReloadInspector();
        }

        private void HandleItemSelect(SoundItemUI target)
        {
            if (_selectedItem != null)
                _selectedItem.IsActive = false;

            _selectedItem = target;
            _selectedItem.IsActive = true;
            ReloadInspector();
        }

        private void HandleItemDelete(SoundItemUI target)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                L10n.DeleteTitle, L10n.DeleteMessage(target.Name), L10n.Yes, L10n.No);
            if (!confirmed) return;

            soundListSo.RemoveSound(target.SoundItem);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(target.SoundItem));
            EditorUtility.SetDirty(soundListSo);
            AssetDatabase.SaveAssets();

            if (_selectedItem == target)
            {
                _selectedItem = null;
                ShowSelectPrompt();
            }
            GeneratePoolItems();
        }

        private void GeneratePoolItems()
        {
            _itemView.Clear();
            _itemList.Clear();

            var list = soundListSo?.GetSoundList();
            if (list == null || list.Count == 0)
            {
                ShowEmptyList();
                if (_selectedItem == null) ShowSelectPrompt();
                return;
            }

            foreach (var item in list)
            {
                if (item == null) continue;
                TemplateContainer itemTemplate = sounditemAsset.Instantiate();
                SoundItemUI itemUI = new SoundItemUI(itemTemplate, item);
                _itemView.Add(itemTemplate);
                _itemList.Add(itemUI);

                itemUI.Name = item.soundName;
                if (_selectedItem != null && _selectedItem.SoundItem == item)
                    HandleItemSelect(itemUI);

                itemUI.OnSelectEvent += HandleItemSelect;
                itemUI.OnDeleteEvent += HandleItemDelete;
            }

            if (_selectedItem == null) ShowSelectPrompt();
        }

        // ── 빈 상태 ──────────────────────────────────────────────────

        private void ShowEmptyList()
        {
            var box = new VisualElement();
            box.AddToClassList("sm-empty");
            box.Add(MakeLabel(L10n.EmptyListTitle, "sm-empty__title"));
            box.Add(MakeLabel(L10n.EmptyListBody, "sm-empty__body"));
            _itemView.Add(box);
        }

        private void ShowSelectPrompt()
        {
            _inspectorView.Clear();
            var box = new VisualElement();
            box.AddToClassList("sm-empty");
            box.Add(MakeLabel("♪", "sm-empty__title"));
            box.Add(MakeLabel(L10n.SelectPrompt, "sm-empty__body"));
            _inspectorView.Add(box);
        }

        // ── 인스펙터 재로드 ──────────────────────────────────────────

        public void ReloadInspector()
        {
            if (_selectedItem?.SoundItem == null) { ShowSelectPrompt(); return; }

            _inspectorView.Clear();
            ResetCachedEditor();

            UnityEditor.Editor.CreateCachedEditor(_selectedItem.SoundItem, null, ref _cachedEditor);
            VisualElement inspectorContent = _cachedEditor.CreateInspectorGUI();

            var serializedObject = new SerializedObject(_selectedItem.SoundItem);
            inspectorContent.Bind(serializedObject);
            inspectorContent.TrackSerializedObjectValue(serializedObject, _ =>
            {
                _selectedItem.Name = _selectedItem.SoundItem.name;
                _selectedItem.RefreshTypeDot();
            });
            _selectedItem.Name = _selectedItem.SoundItem.name;
            _selectedItem.RefreshTypeDot();

            _inspectorView.Add(inspectorContent);
        }

        // ════════════════════════════════════════════════════════════
        // Settings 탭
        // ════════════════════════════════════════════════════════════

        private void RebuildSettingsTab()
        {
            _settingsTab.Clear();
            BuildSettingsTab();
        }

        private void BuildSettingsTab()
        {
            _settingsTab.Clear();

            // ── 풀 ──────────────────────────────────────────────
            var (poolCard, poolBody) = MakeSection(L10n.GroupPool, null);

            var poolSize = new IntegerField(L10n.LabelPoolSize) { value = SoundManagerPrefs.GetPoolSize() };
            poolSize.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Clamp(evt.newValue, 1, 256);
                poolSize.SetValueWithoutNotify(v);
                PlayerPrefs.SetInt(SoundManagerPrefs.PoolSize, v);
            });

            var autoExpand = new Toggle(L10n.LabelAutoExpand) { value = SoundManagerPrefs.GetAutoExpand() };
            autoExpand.RegisterValueChangedCallback(evt =>
                PlayerPrefs.SetInt(SoundManagerPrefs.PoolAutoExpand, evt.newValue ? 1 : 0));

            poolBody.Add(poolSize);
            poolBody.Add(autoExpand);
            AddHint(poolCard, L10n.HintPool);
            _settingsTab.Add(poolCard);

            // ── 볼륨 ────────────────────────────────────────────
            var (volCard, volBody) = MakeSection(L10n.GroupVolume, "sm-section__bar--fade");
            volBody.Add(MakeVolumeRow(L10n.LabelMaster, SoundManagerPrefs.MasterVolume));
            volBody.Add(MakeVolumeRow(L10n.LabelBGM,    SoundManagerPrefs.BGMVolume));
            volBody.Add(MakeVolumeRow(L10n.LabelSFX,    SoundManagerPrefs.SFXVolume));
            AddHint(volCard, L10n.HintVolume);

            var volActions = new VisualElement();
            volActions.style.flexDirection = FlexDirection.Row;
            volActions.style.justifyContent = Justify.FlexEnd;
            volActions.style.paddingRight = 13;
            volActions.style.paddingTop = 2;

            var resetBtn = new Button(ResetVolumes) { text = L10n.ResetBtn };
            resetBtn.AddToClassList("sm-btn"); resetBtn.AddToClassList("sm-btn--ghost");
            var saveBtn = new Button(SaveSettings) { text = L10n.SaveBtn };
            saveBtn.AddToClassList("sm-btn"); saveBtn.AddToClassList("sm-btn--primary");
            volActions.Add(resetBtn);
            volActions.Add(saveBtn);
            volCard.Add(volActions);

            _toast = MakeLabel(string.Empty, "sm-toast");
            volCard.Add(_toast);
            _settingsTab.Add(volCard);

            // ── 데이터 폴더 ─────────────────────────────────────
            var (dataCard, dataBody) = MakeSection(L10n.GroupData, "sm-section__bar--3d");
            var row = new VisualElement();
            row.AddToClassList("sm-row");

            _dataPathField = new TextField { value = SoundManagerConfig.DataPath, isDelayed = true };
            _dataPathField.AddToClassList("sm-pathfield");
            _dataPathField.RegisterValueChangedCallback(evt => ApplyDataPathChange(evt.newValue));

            var browseBtn = new Button(BrowseDataFolder) { text = L10n.BrowseBtn };
            browseBtn.AddToClassList("sm-btn"); browseBtn.AddToClassList("sm-btn--ghost");

            row.Add(_dataPathField);
            row.Add(browseBtn);
            dataBody.Add(row);
            _settingsTab.Add(dataCard);
        }

        private VisualElement MakeVolumeRow(string label, string prefsKey)
        {
            var row = new VisualElement();
            row.AddToClassList("sm-vol");

            var lab = MakeLabel(label, "sm-vol__label");

            float initial = PlayerPrefs.GetFloat(prefsKey, SoundManagerPrefs.DefaultVolume);
            var slider = new Slider(0f, 1f) { value = initial };
            slider.AddToClassList("sm-vol__slider");

            var valueLabel = MakeLabel(Mathf.RoundToInt(initial * 100f) + "%", "sm-vol__value");

            slider.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetFloat(prefsKey, evt.newValue);
                valueLabel.text = Mathf.RoundToInt(evt.newValue * 100f) + "%";
                ApplyVolumeLive(prefsKey, evt.newValue);
            });

            row.Add(lab);
            row.Add(slider);
            row.Add(valueLabel);
            return row;
        }

        private void ApplyVolumeLive(string prefsKey, float value)
        {
            if (!Application.isPlaying) return;
            var mgr = UnityEngine.Object.FindAnyObjectByType<RunTime.SoundManager>();
            if (mgr == null) return;
            if (prefsKey == SoundManagerPrefs.MasterVolume) mgr.SetMasterVolume(value);
            else if (prefsKey == SoundManagerPrefs.BGMVolume) mgr.SetBGMVolume(value);
            else if (prefsKey == SoundManagerPrefs.SFXVolume) mgr.SetSFXVolume(value);
        }

        private void ResetVolumes()
        {
            PlayerPrefs.SetFloat(SoundManagerPrefs.MasterVolume, SoundManagerPrefs.DefaultVolume);
            PlayerPrefs.SetFloat(SoundManagerPrefs.BGMVolume, SoundManagerPrefs.DefaultVolume);
            PlayerPrefs.SetFloat(SoundManagerPrefs.SFXVolume, SoundManagerPrefs.DefaultVolume);
            RebuildSettingsTab();
            ShowToast();
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
            ShowToast();
        }

        private void ShowToast()
        {
            if (_toast == null) return;
            _toast.text = L10n.SavedToast;
            _toast.EnableInClassList("sm-toast--show", true);
            // 1.5초 뒤 사라지게
            _toast.schedule.Execute(() => _toast.EnableInClassList("sm-toast--show", false)).StartingIn(1500);
        }

        // ── 데이터 경로 ──────────────────────────────────────────────

        private void BrowseDataFolder()
        {
            string selected = EditorUtility.OpenFolderPanel(L10n.GroupData, "Assets", "");
            if (string.IsNullOrEmpty(selected)) return;

            if (!selected.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog(L10n.Warning, L10n.PathOutsideAssets, L10n.Ok);
                return;
            }

            string relative = "Assets" + selected.Substring(Application.dataPath.Length);
            ApplyDataPathChange(relative);
            _dataPathField.SetValueWithoutNotify(relative);
        }

        private void ApplyDataPathChange(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath)) return;
            SoundManagerConfig.DataPath = newPath;

            soundListSo = null;
            _selectedItem = null;
            LoadSoundListSO();
            GeneratePoolItems();
        }

        // ════════════════════════════════════════════════════════════
        // 공통 UI 빌더
        // ════════════════════════════════════════════════════════════

        private (VisualElement card, VisualElement body) MakeSection(string title, string barModifier)
        {
            var card = new VisualElement();
            card.AddToClassList("sm-section");

            var header = new VisualElement();
            header.AddToClassList("sm-section__header");
            var bar = new VisualElement();
            bar.AddToClassList("sm-section__bar");
            if (!string.IsNullOrEmpty(barModifier)) bar.AddToClassList(barModifier);
            header.Add(bar);
            header.Add(MakeLabel(title, "sm-section__title"));
            card.Add(header);

            var body = new VisualElement();
            body.AddToClassList("sm-section__body");
            card.Add(body);

            return (card, body);
        }

        private void AddHint(VisualElement card, string text)
            => card.Add(MakeLabel(text, "sm-hint"));

        private static Label MakeLabel(string text, string ussClass)
        {
            var l = new Label(text);
            if (!string.IsNullOrEmpty(ussClass)) l.AddToClassList(ussClass);
            return l;
        }

        // ── SoundListSO 로드/생성 ────────────────────────────────────

        private void LoadSoundListSO()
        {
            if (soundListSo != null) return;

            SoundManagerConfig.EnsureDataFolderExists();
            soundListSo = AssetDatabase.LoadAssetAtPath<SoundListSo>(SoundManagerConfig.SoundListSOPath);
            if (soundListSo == null)
            {
                Debug.LogWarning($"[SoundManager] SoundListSO not found at '{SoundManagerConfig.SoundListSOPath}'. Creating a new one.");
                soundListSo = ScriptableObject.CreateInstance<SoundListSo>();
                AssetDatabase.CreateAsset(soundListSo, SoundManagerConfig.SoundListSOPath);
                AssetDatabase.SaveAssets();
            }
        }

        // ── 생명주기 ────────────────────────────────────────────────

        private void OnEnable()          => ResetCachedEditor();
        private void OnBecameInvisible() => ResetCachedEditor();

        private void ResetCachedEditor()
        {
            if (_cachedEditor is SoundSOEditor soundEditor)
                soundEditor.ResetSound();
        }

        // ── 초기화 (패키지 내부 파일) ───────────────────────────────

        private void InitializeWindow()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            _packageRootPath = Directory.GetParent(Path.GetDirectoryName(scriptPath)).FullName.Replace("\\", "/");
            _packageRootPath = "Assets" + _packageRootPath.Substring(Application.dataPath.Length);

            visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{_packageRootPath}/Editor/UIS/SoundListUIs/SoundListEditor.uxml");
            Debug.Assert(visualTreeAsset != null, "SoundListEditor.uxml is null");

            sounditemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{_packageRootPath}/Editor/UIS/SoundListUIs/SoundItemUI.uxml");
            Debug.Assert(sounditemAsset != null, "SoundItemUI.uxml is null");

            themeStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                $"{_packageRootPath}/Editor/UIS/SoundManagerTheme.uss");
        }
    }
}
