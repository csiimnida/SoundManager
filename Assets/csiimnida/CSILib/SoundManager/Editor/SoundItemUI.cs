using System;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace csiimnida.CSILib.SoundManager.Editor
{
    public class SoundItemUI
    {
        private static readonly Color ColorBgm = new(0.72f, 0.58f, 0.96f);
        private static readonly Color ColorSfx = new(0.96f, 0.77f, 0.33f);
        private static readonly Color ColorSelected = new(0.17f, 0.36f, 0.53f, 0.45f);

        private readonly Label _nameLabel;
        private readonly Button _deleteBtn;
        private readonly VisualElement _rootElement;
        private readonly VisualElement _typeDot;
        private bool _isActive;

        public event Action<SoundItemUI> OnDeleteEvent;
        public event Action<SoundItemUI> OnSelectEvent;

        public SoundSo SoundItem;

        public string Name
        {
            get => _nameLabel.text;
            set
            {
                _nameLabel.text = value;
                _nameLabel.tooltip = value;
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                _rootElement.style.backgroundColor = value ? ColorSelected : StyleKeyword.Null;
            }
        }

        public SoundItemUI(VisualElement root, SoundSo item)
        {
            SoundItem = item;
            _rootElement = root.Q<VisualElement>("SoundItem");
            _nameLabel = _rootElement.Q<Label>("SoundName");
            _deleteBtn = _rootElement.Q<Button>("DeleteBtn");
            _typeDot = _rootElement.Q<VisualElement>("TypeDot");

            root.style.width = Length.Percent(100);
            root.style.flexShrink = 0;
            ApplyListItemLayout();

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

        private void ApplyListItemLayout()
        {
            _rootElement.style.width = Length.Percent(100);
            _rootElement.style.minWidth = 0;
            _rootElement.style.overflow = Overflow.Hidden;

            VisualElement nameContainer = _rootElement.Q<VisualElement>("NameContainer");
            if (nameContainer != null)
            {
                nameContainer.style.flexGrow = 1;
                nameContainer.style.flexShrink = 1;
                nameContainer.style.minWidth = 0;
                nameContainer.style.overflow = Overflow.Hidden;
            }

            _nameLabel.style.flexGrow = 1;
            _nameLabel.style.flexShrink = 1;
            _nameLabel.style.minWidth = 0;
            _nameLabel.style.overflow = Overflow.Hidden;
            _nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            _nameLabel.style.whiteSpace = WhiteSpace.NoWrap;

            _deleteBtn.style.flexShrink = 0;
            _deleteBtn.style.width = 22;
            _deleteBtn.style.minWidth = 22;
        }

        public void RefreshTypeDot()
        {
            if (_typeDot == null || SoundItem == null) return;
            _typeDot.style.backgroundColor = SoundItem.soundType == SoundType.BGM ? ColorBgm : ColorSfx;
        }
    }
}
