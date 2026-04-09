using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    public sealed class CollectionScreenController
    {
        private readonly List<SeedDefinition> _seeds = new()
        {
            new SeedDefinition("Acaí", "Semilla asociada a la energía vital. Representa movimiento, resistencia y conexión con el territorio."),
            new SeedDefinition("Copoazú", "Semilla vinculada al bienestar y al cuidado. Refuerza la idea de protección y vida."),
            new SeedDefinition("Uva Caimarona", "Semilla relacionada con la defensa y la resistencia. Su historia refuerza el valor del conocimiento natural."),
            new SeedDefinition("Sacha Inchi", "Semilla asociada a la claridad mental y al aprendizaje. Invita a explorar y descubrir."),
            new SeedDefinition("Chontaduro", "Semilla ligada a la fuerza ancestral. Su valor cultural acompaña el cierre del recorrido educativo."),
        };

        private readonly SlotBinding[] _slots = new SlotBinding[3];

        private GameObject _overlayContainer;
        private GameObject _detailPanel;
        private TMP_Text _detailName;
        private TMP_Text _detailDescription;
        private Button _carouselLeftButton;
        private Button _carouselRightButton;
        private Button _openDetailButton;
        private Button _detailLeftButton;
        private Button _detailRightButton;

        private int _centerSeedIndex;
        private int _selectedSeedIndex;

        public bool IsDetailOpen => _detailPanel != null && _detailPanel.activeSelf;

        public void Initialize(Transform collectionScreenRoot)
        {
            if (collectionScreenRoot == null)
            {
                throw new ArgumentNullException(nameof(collectionScreenRoot));
            }

            _carouselLeftButton = UIPathUtility.EnsureButton(collectionScreenRoot, "CarouselRoot/Left");
            _carouselRightButton = UIPathUtility.EnsureButton(collectionScreenRoot, "CarouselRoot/Right");
            _openDetailButton = UIPathUtility.EnsureButton(collectionScreenRoot, "CarouselRoot/Info");
            _detailLeftButton = UIPathUtility.EnsureButton(collectionScreenRoot, "OverlayContainer/DetailPanel/Panel/Left");
            _detailRightButton = UIPathUtility.EnsureButton(collectionScreenRoot, "OverlayContainer/DetailPanel/Panel/Right");

            _overlayContainer = UIPathUtility.FindRequired(collectionScreenRoot, "OverlayContainer").gameObject;
            _detailPanel = UIPathUtility.FindRequired(collectionScreenRoot, "OverlayContainer/DetailPanel").gameObject;
            _detailName = UIPathUtility.FindRequired(collectionScreenRoot, "OverlayContainer/DetailPanel/Panel/SeedName").GetComponent<TMP_Text>();
            _detailDescription = UIPathUtility.FindRequired(collectionScreenRoot, "OverlayContainer/DetailPanel/Panel/SeedDescription").GetComponent<TMP_Text>();

            _slots[0] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotLeft/SeeCard");
            _slots[1] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotCenter/SeeCard");
            _slots[2] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotRight/SeeCard");

            _carouselLeftButton.onClick.RemoveAllListeners();
            _carouselRightButton.onClick.RemoveAllListeners();
            _openDetailButton.onClick.RemoveAllListeners();
            _detailLeftButton.onClick.RemoveAllListeners();
            _detailRightButton.onClick.RemoveAllListeners();

            _carouselLeftButton.onClick.AddListener(ShowPreviousSeed);
            _carouselRightButton.onClick.AddListener(ShowNextSeed);
            _openDetailButton.onClick.AddListener(OpenCenterSeedDetail);
            _detailLeftButton.onClick.AddListener(ShowPreviousDetail);
            _detailRightButton.onClick.AddListener(ShowNextDetail);

            _overlayContainer.SetActive(false);
            _detailPanel.SetActive(false);
            _centerSeedIndex = 0;
            RefreshCarousel();
        }

        public bool HandleBackRequested()
        {
            if (!IsDetailOpen)
            {
                return false;
            }

            CloseDetail();
            return true;
        }

        public void OpenCenterSeedDetail()
        {
            OpenDetail(_centerSeedIndex);
        }

        public void CloseDetail()
        {
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(false);
            }

            if (_overlayContainer != null)
            {
                _overlayContainer.SetActive(false);
            }
        }

        private SlotBinding BuildSlot(Transform collectionScreenRoot, string cardPath)
        {
            Transform cardRoot = UIPathUtility.FindRequired(collectionScreenRoot, cardPath);
            Button button = UIPathUtility.EnsureButton(cardRoot);
            TMP_Text label = cardRoot.GetComponentInChildren<TMP_Text>(true);

            button.onClick.RemoveAllListeners();

            return new SlotBinding(button, label);
        }

        private void ShowPreviousSeed()
        {
            _centerSeedIndex = Wrap(_centerSeedIndex - 1);
            RefreshCarousel();
        }

        private void ShowNextSeed()
        {
            _centerSeedIndex = Wrap(_centerSeedIndex + 1);
            RefreshCarousel();
        }

        private void ShowPreviousDetail()
        {
            OpenDetail(Wrap(_selectedSeedIndex - 1));
        }

        private void ShowNextDetail()
        {
            OpenDetail(Wrap(_selectedSeedIndex + 1));
        }

        private void OpenDetail(int seedIndex)
        {
            _selectedSeedIndex = Wrap(seedIndex);
            _centerSeedIndex = _selectedSeedIndex;

            SeedDefinition seed = _seeds[_selectedSeedIndex];
            _detailName.text = seed.Name;
            _detailDescription.text = seed.Description;
            _overlayContainer.SetActive(true);
            _detailPanel.SetActive(true);

            RefreshCarousel();
        }

        private void RefreshCarousel()
        {
            int leftIndex = Wrap(_centerSeedIndex - 1);
            int rightIndex = Wrap(_centerSeedIndex + 1);

            BindSlot(_slots[0], leftIndex);
            BindSlot(_slots[1], _centerSeedIndex);
            BindSlot(_slots[2], rightIndex);
        }

        private void BindSlot(SlotBinding slot, int seedIndex)
        {
            SeedDefinition seed = _seeds[seedIndex];

            if (slot.Label != null)
            {
                slot.Label.text = seed.Name;
            }

            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => OpenDetail(seedIndex));
        }

        private int Wrap(int index)
        {
            int count = _seeds.Count;
            return (index % count + count) % count;
        }

        private readonly struct SlotBinding
        {
            public SlotBinding(Button button, TMP_Text label)
            {
                Button = button;
                Label = label;
            }

            public Button Button { get; }
            public TMP_Text Label { get; }
        }

        private readonly struct SeedDefinition
        {
            public SeedDefinition(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; }
            public string Description { get; }
        }
    }
}
