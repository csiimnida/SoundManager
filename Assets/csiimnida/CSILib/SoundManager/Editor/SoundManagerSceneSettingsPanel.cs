using System;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace csiimnida.CSILib.SoundManager.Editor
{
    using L10n = EditorLocalization;
    using RuntimeSoundManager = csiimnida.CSILib.SoundManager.RunTime.SoundManager;

    /// <summary>
    /// SoundListEditor Settings 탭 — 씬 SoundManager 컴포넌트 인스펙터와 동일한 필드를 UIToolkit으로 편집합니다.
    /// </summary>
    internal sealed class SoundManagerSceneSettingsPanel
    {
        private readonly Func<SoundListSo> _getEditorSoundList;
        private readonly Action _onSettingsChanged;

        private Foldout _foldout;
        private SerializedObject _serializedManager;
        private RuntimeSoundManager _targetManager;

        public SoundManagerSceneSettingsPanel(Func<SoundListSo> getEditorSoundList, Action onSettingsChanged)
        {
            _getEditorSoundList = getEditorSoundList;
            _onSettingsChanged = onSettingsChanged;
        }

        public void Build(VisualElement parent)
        {
            _foldout = new Foldout { text = L10n.GroupSceneManager, value = true };
            _foldout.Add(new HelpBox(L10n.HintSceneManager, HelpBoxMessageType.Info));
            RebuildBody();
            parent.Add(_foldout);
        }

        public void Refresh()
        {
            if (_foldout == null) return;
            RebuildBody();
        }

        public void SyncSoundListToAllManagers()
        {
            SoundListSo list = _getEditorSoundList?.Invoke();
            if (list == null) return;

            foreach (RuntimeSoundManager manager in FindSceneManagers())
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("soundListSo").objectReferenceValue = list;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
            }

            Refresh();
            _onSettingsChanged?.Invoke();
        }

        private void RebuildBody()
        {
            int headerCount = _foldout.childCount;
            for (int i = headerCount - 1; i >= 1; i--)
                _foldout.RemoveAt(i);

            RefreshTarget();

            if (_targetManager == null)
            {
                _foldout.Add(new HelpBox(L10n.NoSceneManager, HelpBoxMessageType.Warning));

                var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
                actions.Add(new Button(CreateSceneManager) { text = L10n.CreateSceneManagerBtn });
                _foldout.Add(actions);
                return;
            }

            int managerCount = FindSceneManagers().Length;
            if (managerCount > 1)
                _foldout.Add(new HelpBox(L10n.MultipleSceneManagers, HelpBoxMessageType.Warning));

            _serializedManager = new SerializedObject(_targetManager);

            var syncRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            syncRow.Add(new Button(SyncSoundListToAllManagers) { text = L10n.SyncSoundListBtn });
            _foldout.Add(syncRow);

            AddBoundProperty("soundListSo", L10n.LabelSoundListSo);
            AddBoundProperty("mixer", L10n.LabelAudioMixer);
            AddBoundProperty("bgmGroup", L10n.LabelBgmGroup);
            AddBoundProperty("sfxGroup", L10n.LabelSfxGroup);

            var mixerParams = new Foldout { text = L10n.GroupMixerParams, value = false };
            AddBoundPropertyTo(mixerParams, "masterVolumeParam", L10n.LabelMasterParam);
            AddBoundPropertyTo(mixerParams, "bgmVolumeParam", L10n.LabelBgmParam);
            AddBoundPropertyTo(mixerParams, "sfxVolumeParam", L10n.LabelSfxParam);
            _foldout.Add(mixerParams);

            _foldout.Add(new Label($"{L10n.SceneTargetLabel}: {_targetManager.gameObject.name}")
            {
                style = { fontSize = 11, marginTop = 4 }
            });
        }

        private void AddBoundProperty(string propertyName, string label)
            => AddBoundPropertyTo(_foldout, propertyName, label);

        private void AddBoundPropertyTo(VisualElement parent, string propertyName, string label)
        {
            SerializedProperty property = _serializedManager.FindProperty(propertyName);
            if (property == null) return;

            var field = new PropertyField(property, label);
            field.Bind(_serializedManager);
            field.RegisterValueChangeCallback(_ => ApplyManagerChanges());
            parent.Add(field);
        }

        private void ApplyManagerChanges()
        {
            if (_serializedManager == null || _targetManager == null || _serializedManager.targetObject == null)
            {
                Refresh();
                return;
            }

            _serializedManager.ApplyModifiedProperties();
            if (_targetManager == null)
            {
                Refresh();
                return;
            }

            EditorUtility.SetDirty(_targetManager);
            _onSettingsChanged?.Invoke();
        }

        private void RefreshTarget()
        {
            RuntimeSoundManager[] managers = FindSceneManagers();
            _targetManager = managers.Length > 0 ? managers[0] : null;
        }

        private static RuntimeSoundManager[] FindSceneManagers()
            => Object.FindObjectsByType<RuntimeSoundManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private void CreateSceneManager()
        {
            var go = new GameObject("SoundManager");
            Undo.RegisterCreatedObjectUndo(go, "Create SoundManager");
            RuntimeSoundManager manager = go.AddComponent<RuntimeSoundManager>();

            SoundListSo list = _getEditorSoundList?.Invoke();
            if (list != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("soundListSo").objectReferenceValue = list;
                so.ApplyModifiedProperties();
            }

            Selection.activeGameObject = go;
            Refresh();
            _onSettingsChanged?.Invoke();
        }
    }
}
