using System;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    public sealed class CharacterSelectController
    {
        private Button _characterOneButton;
        private Button _characterTwoButton;
        private Button _confirmSelectionButton;
        private Graphic _characterOneGraphic;
        private Graphic _characterTwoGraphic;
        private int _selectedCharacterIndex;

        public void Initialize(Transform characterSelectScreenRoot, Action onSelectionConfirmed)
        {
            if (characterSelectScreenRoot == null)
            {
                throw new ArgumentNullException(nameof(characterSelectScreenRoot));
            }

            _characterOneButton = UIPathUtility.EnsureButton(characterSelectScreenRoot, "Character");
            _characterTwoButton = UIPathUtility.EnsureButton(characterSelectScreenRoot, "Character_2");
            _confirmSelectionButton = UIPathUtility.EnsureButton(characterSelectScreenRoot, "Selection");

            _characterOneGraphic = _characterOneButton.targetGraphic != null
                ? _characterOneButton.targetGraphic
                : _characterOneButton.GetComponent<Graphic>();

            _characterTwoGraphic = _characterTwoButton.targetGraphic != null
                ? _characterTwoButton.targetGraphic
                : _characterTwoButton.GetComponent<Graphic>();

            _characterOneButton.onClick.RemoveAllListeners();
            _characterTwoButton.onClick.RemoveAllListeners();
            _confirmSelectionButton.onClick.RemoveAllListeners();

            _characterOneButton.onClick.AddListener(() => SelectCharacter(0));
            _characterTwoButton.onClick.AddListener(() => SelectCharacter(1));
            _confirmSelectionButton.onClick.AddListener(() => onSelectionConfirmed?.Invoke());

            SelectCharacter(0);
        }

        private void SelectCharacter(int characterIndex)
        {
            _selectedCharacterIndex = characterIndex;

            ApplySelectionState(_characterOneButton.transform, _characterOneGraphic, characterIndex == 0);
            ApplySelectionState(_characterTwoButton.transform, _characterTwoGraphic, characterIndex == 1);
        }

        private static void ApplySelectionState(Transform characterRoot, Graphic graphic, bool isSelected)
        {
            characterRoot.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;

            if (graphic != null)
            {
                graphic.color = isSelected
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.82f, 0.82f, 0.82f, 1f);
            }
        }
    }
}
