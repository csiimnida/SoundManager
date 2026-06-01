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
        // 패키지 내부 UXML 경로 — 업데이트로 갱신되어도 무관
        private string _packageRootPath;

        private VisualTreeAsset visualTreeAsset;
        private VisualTreeAsset sounditemAsset;

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
        private SoundManagerSceneSettingsPanel _sceneSettingsPanel;

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
            visualTreeAsset.CloneTree(root);

            BindElements(root);
            BuildSettingsTab();
            ApplyTranslations();

            LoadSoundListSO();
            RefreshSceneSettingsPanel();
            GeneratePoolItems();
            ShowTab(true);
        }

        // ── 요소 바인딩 ─────────────────────────────────────────────

        private void BindElements(VisualElement root)
        {
            VisualElement topbar = root.Q<VisualElement>("Topbar");
            if (topbar != null)
            {
                topbar.style.flexShrink = 0;
                topbar.style.height = 30;
                topbar.style.minHeight = 30;
            }

            VisualElement tabBar = root.Q<VisualElement>("TabBar");
            if (tabBar != null)
            {
                tabBar.style.flexShrink = 0;
                tabBar.style.height = 28;
                tabBar.style.minHeight = 28;
            }

            _createBtn = root.Q<Button>("CreateBtn");
            _createBtn.clicked += HandleCreateBtn;
            _createBtn.style.width = 28;
            _createBtn.style.minWidth = 28;
            _createBtn.style.maxWidth = 28;
            _createBtn.style.height = 22;
            _createBtn.style.minHeight = 22;
            _createBtn.style.flexShrink = 0;

            _langBtn = root.Q<Button>("LenBtn");
            _langBtn.clicked += HandleLangBtn;
            _langBtn.style.width = 92;
            _langBtn.style.minWidth = 92;
            _langBtn.style.maxWidth = 92;
            _langBtn.style.height = 22;
            _langBtn.style.minHeight = 22;
            _langBtn.style.flexShrink = 0;

            _tabSoundsBtn   = root.Q<Button>("TabSounds");
            _tabSettingsBtn = root.Q<Button>("TabSettings");
            _tabSoundsBtn.clicked   += () => ShowTab(true);
            _tabSettingsBtn.clicked += () => ShowTab(false);
            _tabSoundsBtn.style.flexGrow = 1;
            _tabSoundsBtn.style.flexBasis = 0;
            _tabSoundsBtn.style.minWidth = 120;
            _tabSoundsBtn.style.height = 22;
            _tabSoundsBtn.style.minHeight = 22;
            _tabSettingsBtn.style.flexGrow = 1;
            _tabSettingsBtn.style.flexBasis = 0;
            _tabSettingsBtn.style.minWidth = 120;
            _tabSettingsBtn.style.height = 22;
            _tabSettingsBtn.style.minHeight = 22;

            _soundsTab     = root.Q<VisualElement>("SoundsTab");
            _settingsTab   = root.Q<ScrollView>("SettingsTab");
            _itemView      = root.Q<ScrollView>("ItemView");
            _inspectorView = root.Q<ScrollView>("InspectorView");
            _soundsTab.style.minWidth = 0;
            _soundsTab.style.overflow = Overflow.Hidden;

            VisualElement leftPanel = root.Q<VisualElement>("LeftPanel");
            if (leftPanel != null)
            {
                leftPanel.style.minWidth = 0;
                leftPanel.style.overflow = Overflow.Hidden;
            }
            VisualElement rightPanel = root.Q<VisualElement>("RightPanel");
            if (rightPanel != null)
            {
                rightPanel.style.minWidth = 0;
                rightPanel.style.overflow = Overflow.Hidden;
            }

            _itemView.mode = ScrollViewMode.Vertical;
            _itemView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _itemView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _itemView.contentContainer.style.width = Length.Percent(100);
            _itemView.contentContainer.style.minWidth = 0;
            _inspectorView.mode = ScrollViewMode.Vertical;
            _inspectorView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _inspectorView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _inspectorView.contentContainer.style.minWidth = 0;
            _settingsTab.mode = ScrollViewMode.Vertical;
            _settingsTab.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _settingsTab.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

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
            SetTabActive(_tabSoundsBtn, sounds);
            SetTabActive(_tabSettingsBtn, !sounds);
            _createBtn.style.display = sounds ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetTabActive(Button tab, bool active)
        {
            // Bold 전환은 텍스트 폭이 바뀌어 탭이 미세하게 밀릴 수 있어 색상만 변경한다.
            tab.style.unityFontStyleAndWeight = FontStyle.Normal;
            tab.style.backgroundColor = active
                ? new Color(0.24f, 0.34f, 0.48f, 0.8f)
                : StyleKeyword.Null;
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
            _sceneSettingsPanel?.SyncSoundListToAllManagers();
            GeneratePoolItems();
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
            _itemView.Add(new HelpBox($"{L10n.EmptyListTitle}\n{L10n.EmptyListBody}", HelpBoxMessageType.Info));
        }

        private void ShowSelectPrompt()
        {
            _inspectorView.Clear();
            _inspectorView.Add(new HelpBox(L10n.SelectPrompt, HelpBoxMessageType.Info));
        }

        // ── 인스펙터 재로드 ──────────────────────────────────────────

        public void ReloadInspector()
        {
            if (_selectedItem?.SoundItem == null) { ShowSelectPrompt(); return; }

            _inspectorView.Clear();
            ResetCachedEditor();

            UnityEditor.Editor.CreateCachedEditor(_selectedItem.SoundItem, null, ref _cachedEditor);
            VisualElement inspectorContent = _cachedEditor.CreateInspectorGUI();
            inspectorContent.style.width = Length.Percent(100);
            inspectorContent.style.minWidth = 0;
            inspectorContent.style.flexGrow = 1;

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

        private void RefreshSceneSettingsPanel()
            => _sceneSettingsPanel?.Refresh();

        private void BuildSettingsTab()
        {
            _settingsTab.Clear();

            _sceneSettingsPanel ??= new SoundManagerSceneSettingsPanel(
                () => soundListSo,
                () => { /* scene fields saved via SerializedObject */ });
            _sceneSettingsPanel.Build(_settingsTab);

            var poolFoldout = new Foldout { text = L10n.GroupPool, value = true };
            poolFoldout.Add(new HelpBox(L10n.HintPool, HelpBoxMessageType.None));

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

            poolFoldout.Add(poolSize);
            poolFoldout.Add(autoExpand);
            _settingsTab.Add(poolFoldout);

            var volFoldout = new Foldout { text = L10n.GroupVolume, value = true };
            volFoldout.Add(new HelpBox(L10n.HintVolume, HelpBoxMessageType.None));
            volFoldout.Add(MakeVolumeRow(L10n.LabelMaster, SoundManagerPrefs.MasterVolume));
            volFoldout.Add(MakeVolumeRow(L10n.LabelBGM, SoundManagerPrefs.BGMVolume));
            volFoldout.Add(MakeVolumeRow(L10n.LabelSFX, SoundManagerPrefs.SFXVolume));

            var volActions = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 6 }
            };
            volActions.Add(new Button(ResetVolumes) { text = L10n.ResetBtn });
            volActions.Add(new Button(SaveSettings) { text = L10n.SaveBtn });
            volFoldout.Add(volActions);

            _toast = new Label { style = { display = DisplayStyle.None, marginTop = 4 } };
            volFoldout.Add(_toast);
            _settingsTab.Add(volFoldout);

            var dataFoldout = new Foldout { text = L10n.GroupData, value = true };
            var pathRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            _dataPathField = new TextField(L10n.SettingsLabel) { value = SoundManagerConfig.DataPath, isDelayed = true };
            _dataPathField.style.flexGrow = 1;
            _dataPathField.RegisterValueChangedCallback(evt => ApplyDataPathChange(evt.newValue));

            var browseBtn = new Button(BrowseDataFolder) { text = L10n.BrowseBtn };
            pathRow.Add(_dataPathField);
            pathRow.Add(browseBtn);
            dataFoldout.Add(pathRow);
            _settingsTab.Add(dataFoldout);
        }

        private VisualElement MakeVolumeRow(string label, string prefsKey)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            var lab = new Label(label) { style = { width = 72, flexShrink = 0 } };

            float initial = PlayerPrefs.GetFloat(prefsKey, SoundManagerPrefs.DefaultVolume);
            var slider = new Slider(0f, 1f) { value = initial, style = { flexGrow = 1 } };
            var valueLabel = new Label(Mathf.RoundToInt(initial * 100f) + "%") { style = { width = 40, flexShrink = 0 } };

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
            _toast.style.display = DisplayStyle.Flex;
            _toast.schedule.Execute(() => _toast.style.display = DisplayStyle.None).StartingIn(1500);
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
            RefreshSceneSettingsPanel();
            GeneratePoolItems();
        }

        // ── SoundListSO 로드/생성 ────────────────────────────────────

        private void LoadSoundListSO()
        {
            if (soundListSo == null)
            {
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

            RefreshSceneSettingsPanel();
        }

        // ── 생명주기 ────────────────────────────────────────────────

        private void OnEnable()          => ResetCachedEditor();
        private void OnBecameInvisible() => ResetCachedEditor();

        private void OnDisable()
        {
            // 도메인 리로드/창 닫힘 시 임베드한 에디터를 확실히 정리해 댕글링 참조를 방지
            ResetCachedEditor();
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }
        }

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
        }
    }
}
