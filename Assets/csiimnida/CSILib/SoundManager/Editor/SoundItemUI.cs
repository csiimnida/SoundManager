using System;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace csiimnida.CSILib.SoundManager.Editor
{
    public class SoundItemUI
    {
        private readonly Label _nameLabel;
        private readonly Button _deleteBtn;
        private readonly VisualElement _rootElement;
        private readonly VisualElement _typeDot;

        public event Action<SoundItemUI> OnDeleteEvent;
        public event Action<SoundItemUI> OnSelectEvent;

        public SoundSo SoundItem;

        public string Name
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        public bool IsActive
        {
            get => _rootElement.ClassListContains("sm-item--active");
            set => _rootElement.EnableInClassList("sm-item--active", value);
        }

        public SoundItemUI(VisualElement root, SoundSo item)
        {
            SoundItem = item;
            _rootElement = root.Q<VisualElement>("SoundItem");
            _nameLabel   = _rootElement.Q<Label>("SoundName");
            _deleteBtn   = _rootElement.Q<Button>("DeleteBtn");
            _typeDot     = _rootElement.Q<VisualElement>("TypeDot");

            RefreshTypeDot();

            _deleteBtn.RegisterCallback<ClickEvent>(evt =>
            {
                OnDeleteEvent?.Invoke(this);
                evt.StopPropagation();
            });

            _rootElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnSelectEvent?.Invoke(this);
                evt.StopPropagation();
            });
        }

        /// <summary>사운드 타입에 따라 좌측 점 색을 갱신합니다. (BGM=보라, SFX=앰버)</summary>
        public void RefreshTypeDot()
        {
            if (_typeDot == null || SoundItem == null) return;
            Color color = SoundItem.soundType == SoundType.BGM
                ? new Color(0.72f, 0.58f, 0.96f)   // BGM
                : new Color(0.96f, 0.77f, 0.33f);   // SFX
            _typeDot.style.backgroundColor = color;
        }
    }
}
