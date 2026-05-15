using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    
    public sealed class CollectionScreenController
    {
        
        private readonly List<SeedDefinition> _seeds = new()
        {
            new SeedDefinition("Acaí"),
            new SeedDefinition("Chontaduro"),
            new SeedDefinition("Copoazú"),
            new SeedDefinition("Sacha Inchi"),
            new SeedDefinition("Uva Caimora"),
            new SeedDefinition("Coca"),
        };

        private readonly SlotBinding[] _slots = new SlotBinding[3];

        private Button  _carouselLeftButton;
        private Button  _carouselRightButton;

        private GameObject _infoSemillasPanel;
        private Image      _infoSemillasImage;

        private Sprite[] _cardSprites;
        private Sprite[] _infoSprites;
        private int _centerSeedIndex;

        public bool IsInfoOpen => _infoSemillasPanel != null && _infoSemillasPanel.activeSelf;

        public void Initialize(Transform collectionScreenRoot,
                               Sprite[] cardSprites = null,
                               Sprite[] infoSprites = null)
        {
            if (collectionScreenRoot == null)
            {
                throw new ArgumentNullException(nameof(collectionScreenRoot));
            }

            _cardSprites = cardSprites;
            _infoSprites = infoSprites;

            _carouselLeftButton  = UIPathUtility.EnsureButton(collectionScreenRoot, "CarouselRoot/Left");
            _carouselRightButton = UIPathUtility.EnsureButton(collectionScreenRoot, "CarouselRoot/Right");

            _slots[0] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotLeft/SeeCard");
            _slots[1] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotCenter/SeeCard");
            _slots[2] = BuildSlot(collectionScreenRoot, "CarouselRoot/VisibleSeeds/SlotRight/SeeCard");

            Transform infoPanelTransform = FindTransformByName(collectionScreenRoot, "InfoSemillas");

            if (infoPanelTransform != null)
            {
                _infoSemillasPanel = infoPanelTransform.gameObject;
                _infoSemillasImage = infoPanelTransform.GetComponent<Image>()
                                  ?? infoPanelTransform.GetComponentInChildren<Image>(true);
                Debug.Log($"[CollectionScreen] ✓ InfoSemillas encontrado en: " +
                          $"'{BuildPath(infoPanelTransform)}' | " +
                          $"Image: {(_infoSemillasImage != null ? _infoSemillasImage.name : "NULL")}");
            }
            else
            {
                Debug.LogError("[CollectionScreen] ✗ InfoSemillas NO encontrado. " +
                               "Asegúrate de que exista un GameObject llamado exactamente " +
                               "'InfoSemillas' en algún lugar bajo CollectionScreen.");
            }

            _carouselLeftButton.onClick.RemoveAllListeners();
            _carouselRightButton.onClick.RemoveAllListeners();
            _carouselLeftButton.onClick.AddListener(ShowPreviousSeed);
            _carouselRightButton.onClick.AddListener(ShowNextSeed);

            if (_infoSemillasPanel != null)
            {
                _infoSemillasPanel.SetActive(false);
            }

            _centerSeedIndex = 0;
            RefreshCarousel();
        }

        public bool HandleBackRequested()
        {
            if (!IsInfoOpen)
            {
                return false;
            }

            CloseInfo();
            return true;
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

        private void RefreshCarousel()
        {
            int leftIndex   = Wrap(_centerSeedIndex - 1);
            int rightIndex  = Wrap(_centerSeedIndex + 1);

            BindSlot(_slots[0], leftIndex);
            BindSlot(_slots[1], _centerSeedIndex);
            BindSlot(_slots[2], rightIndex);
        }

        private void BindSlot(SlotBinding slot, int seedIndex)
        {
            
            if (slot.CardImage != null)
            {
                slot.CardImage.sprite = GetCardSprite(seedIndex);
                
                slot.CardImage.enabled = slot.CardImage.sprite != null;
            }

            slot.Button.onClick.RemoveAllListeners();
            int capturedIndex = seedIndex;
            slot.Button.onClick.AddListener(() => OpenInfo(capturedIndex));
        }

        private void OpenInfo(int seedIndex)
        {
            Debug.Log($"[CollectionScreen] OpenInfo({seedIndex}) — " +
                      $"panel: {(_infoSemillasPanel != null ? _infoSemillasPanel.name : "NULL")} | " +
                      $"image: {(_infoSemillasImage != null ? _infoSemillasImage.name : "NULL")} | " +
                      $"sprite[{seedIndex}]: {(GetInfoSprite(seedIndex) != null ? GetInfoSprite(seedIndex).name : "NULL")}");

            if (_infoSemillasPanel == null)
            {
                Debug.LogError("[CollectionScreen] No se puede abrir InfoSemillas: _infoSemillasPanel es null. " +
                               "Revisa la consola de Awake para ver por qué no se encontró.");
                return;
            }

            if (_infoSemillasImage != null)
            {
                _infoSemillasImage.sprite = GetInfoSprite(seedIndex);
            }

            _infoSemillasPanel.SetActive(true);
            Debug.Log($"[CollectionScreen] InfoSemillas.activeSelf después de SetActive(true): {_infoSemillasPanel.activeSelf}");

            SetNavButtonsVisible(false);
        }

        private void CloseInfo()
        {
            if (_infoSemillasPanel != null)
            {
                _infoSemillasPanel.SetActive(false);
            }

            SetNavButtonsVisible(true);
        }

        private void SetNavButtonsVisible(bool visible)
        {
            if (_carouselLeftButton != null)
            {
                _carouselLeftButton.gameObject.SetActive(visible);
            }

            if (_carouselRightButton != null)
            {
                _carouselRightButton.gameObject.SetActive(visible);
            }
        }

        private Sprite GetCardSprite(int index)
        {
            if (_cardSprites == null || index < 0 || index >= _cardSprites.Length)
            {
                return null;
            }

            return _cardSprites[index];
        }

        private Sprite GetInfoSprite(int index)
        {
            if (_infoSprites == null || index < 0 || index >= _infoSprites.Length)
            {
                return null;
            }

            return _infoSprites[index];
        }

        private SlotBinding BuildSlot(Transform collectionScreenRoot, string cardPath)
        {
            Transform cardRoot = UIPathUtility.FindRequired(collectionScreenRoot, cardPath);
            Button button = UIPathUtility.EnsureButton(cardRoot);
            
            Image img = cardRoot.GetComponent<Image>()
                     ?? cardRoot.GetComponentInChildren<Image>(true);
            button.onClick.RemoveAllListeners();
            return new SlotBinding(button, img);
        }

        private int Wrap(int index)
        {
            int count = _seeds.Count;
            return (index % count + count) % count;
        }

        private readonly struct SlotBinding
        {
            public SlotBinding(Button button, Image cardImage)
            {
                Button    = button;
                CardImage = cardImage;
            }

            public Button Button    { get; }
            public Image  CardImage { get; }
        }

        private readonly struct SeedDefinition
        {
            public SeedDefinition(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private static Transform FindTransformByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                {
                    return all[i];
                }
            }

            return null;
        }

        private static string BuildPath(Transform t)
        {
            if (t == null) return "null";
            string path = t.name;
            Transform parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
