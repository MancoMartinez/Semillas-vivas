using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    public sealed class LevelSelectScreenController
    {
        private const string SamplerObjectName = "LevelSelectCharacterSampler";
        private const string IdleState = "Idle";
        private const string RunState = "Run";
        private const float MaxCharacterSize = 50f;

        private readonly Dictionary<Button, string> _sceneNamesByButton = new();
        private readonly List<Button> _orderedLevelButtons = new();
        private readonly List<Image> _levelButtonImages = new();

        private Sprite _unlockedSprite;
        private Color  _unlockedColor;
        private Sprite _lockedSprite;
        private Color  _lockedColor;
        private bool   _hasUnlockedRef;
        private bool   _hasLockedRef;

        private MonoBehaviour _host;
        private Transform _levelSelectRoot;
        private RectTransform _characterRect;
        private RectTransform _characterParent;
        private Image _characterImage;
        private GameObject _samplerObject;
        private Animator _samplerAnimator;
        private SpriteRenderer _samplerRenderer;
        private Coroutine _movementRoutine;
        private Action<string> _onLevelReached;
        private int _currentLevelIndex = -1;
        private int _unlockedLevelCount = int.MaxValue;

        public void Initialize(
            MonoBehaviour host,
            Transform levelSelectRoot,
            RuntimeAnimatorController playerAnimatorController,
            Action<string> onLevelReached)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _onLevelReached = onLevelReached ?? throw new ArgumentNullException(nameof(onLevelReached));
            _levelSelectRoot = levelSelectRoot ?? throw new ArgumentNullException(nameof(levelSelectRoot));

            _characterRect = UIPathUtility.FindRequired(levelSelectRoot, "Character") as RectTransform;
            _characterParent = _characterRect.parent as RectTransform;
            _characterImage = _characterRect.GetComponent<Image>();

            if (_characterRect == null || _characterParent == null || _characterImage == null)
            {
                throw new MissingReferenceException("Level select character is missing RectTransform or Image components.");
            }

            CreateSampler(playerAnimatorController);
            PlayState(IdleState);
            SyncVisual();
        }

        public void BindLevelButton(Button button, string sceneName, int levelIndex)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            _sceneNamesByButton[button] = sceneName;
            EnsureOrderedButtonCapacity(levelIndex + 1);
            _orderedLevelButtons[levelIndex] = button;

            EnsureImageListCapacity(levelIndex + 1);
            Image img = button.GetComponent<Image>();
            _levelButtonImages[levelIndex] = img;

            if (img != null)
            {
                if (levelIndex == 0 && !_hasUnlockedRef)
                {
                    
                    _unlockedSprite  = img.sprite;
                    _unlockedColor   = img.color;
                    _hasUnlockedRef  = true;
                }
                else if (levelIndex > 0 && !_hasLockedRef)
                {
                    
                    _lockedSprite = img.sprite;
                    _lockedColor  = img.color;
                    _hasLockedRef = true;
                }
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => MoveCharacterToLevel(button));
        }

        public void SetUnlockedLevels(int unlockedLevelCount)
        {
            _unlockedLevelCount = Mathf.Max(1, unlockedLevelCount);

            Debug.Log($"[LevelSelect] SetUnlockedLevels({_unlockedLevelCount}) — " +
                      $"{_orderedLevelButtons.Count} botones. " +
                      $"unlockedSprite={_unlockedSprite?.name} lockedSprite={_lockedSprite?.name}");

            for (int index = 0; index < _orderedLevelButtons.Count; index++)
            {
                Button levelButton = _orderedLevelButtons[index];

                if (levelButton == null)
                {
                    continue;
                }

                bool isUnlocked = index < _unlockedLevelCount;
                levelButton.interactable = isUnlocked;

                Image img = index < _levelButtonImages.Count ? _levelButtonImages[index] : null;

                if (img != null && _hasUnlockedRef && _hasLockedRef)
                {
                    img.sprite = isUnlocked ? _unlockedSprite : _lockedSprite;
                    img.color  = isUnlocked ? _unlockedColor  : _lockedColor;
                    img.SetNativeSize(); 
                }

                Debug.Log($"[LevelSelect] Lvl_{index + 1}: {(isUnlocked ? "DESBLOQUEADO ✓" : "bloqueado 🔒")}");
            }
        }

        public void Tick()
        {
            if (_samplerRenderer == null || _characterImage == null)
            {
                return;
            }

            SyncVisual();
        }

        public void Dispose()
        {
            if (_samplerObject != null)
            {
                UnityEngine.Object.Destroy(_samplerObject);
                _samplerObject = null;
                _samplerAnimator = null;
                _samplerRenderer = null;
            }
        }

        private void MoveCharacterToLevel(Button button)
        {
            if (!_sceneNamesByButton.TryGetValue(button, out string sceneName))
            {
                return;
            }

            int targetIndex = GetButtonIndex(button);

            if (targetIndex < 0 || targetIndex >= _unlockedLevelCount)
            {
                return;
            }

            if (_movementRoutine != null)
            {
                _host.StopCoroutine(_movementRoutine);
            }

            RectTransform levelRect = button.transform as RectTransform;
            _movementRoutine = _host.StartCoroutine(MoveToLevelRoutine(levelRect, sceneName));
        }

        private IEnumerator MoveToLevelRoutine(RectTransform levelRect, string sceneName)
        {
            int targetIndex = GetButtonIndex(levelRect.GetComponent<Button>());
            List<Vector2> pathPoints = BuildPathPoints(targetIndex);

            PlayState(RunState);

            for (int index = 0; index < pathPoints.Count; index++)
            {
                Vector2 segmentStart = _characterRect.anchoredPosition;
                Vector2 segmentTarget = pathPoints[index];
                float duration = Mathf.Max(0.15f, Vector2.Distance(segmentStart, segmentTarget) / 260f);
                float elapsed = 0f;

                SetFacing(segmentTarget.x - segmentStart.x);

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float normalized = Mathf.Clamp01(elapsed / duration);
                    _characterRect.anchoredPosition = Vector2.Lerp(segmentStart, segmentTarget, normalized);
                    yield return null;
                }

                _characterRect.anchoredPosition = segmentTarget;
            }

            _currentLevelIndex = targetIndex;
            PlayState(IdleState);
            _movementRoutine = null;
            _onLevelReached?.Invoke(sceneName);
        }

        private Vector2 ConvertTargetToCharacterSpace(RectTransform levelRect)
        {
            Vector3 worldPoint = levelRect.TransformPoint(levelRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_characterParent, screenPoint, null, out Vector2 localPoint))
            {
                Vector2 anchorReference = new(
                    (_characterRect.anchorMin.x - _characterParent.pivot.x) * _characterParent.rect.width,
                    (_characterRect.anchorMin.y - _characterParent.pivot.y) * _characterParent.rect.height);

                return localPoint - anchorReference;
            }

            return _characterRect.anchoredPosition;
        }

        private void CreateSampler(RuntimeAnimatorController playerAnimatorController)
        {
            if (playerAnimatorController == null)
            {
                Debug.LogWarning("LevelSelectScreenController did not receive a player animator controller.");
                return;
            }

            CleanupOrphanedSamplers();

            _samplerObject = new GameObject(SamplerObjectName);
            _samplerObject.transform.SetParent(_levelSelectRoot, false);
            _samplerObject.transform.localPosition = new Vector3(10000f, 10000f, 0f);
            _samplerRenderer = _samplerObject.AddComponent<SpriteRenderer>();
            _samplerRenderer.enabled = false;
            _samplerAnimator = _samplerObject.AddComponent<Animator>();
            _samplerAnimator.runtimeAnimatorController = playerAnimatorController;
            _samplerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        private static void CleanupOrphanedSamplers()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject candidate = allObjects[index];

                if (candidate == null || candidate.name != SamplerObjectName)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(candidate);
            }
        }

        private void PlayState(string stateName)
        {
            if (_samplerAnimator == null)
            {
                return;
            }

            _samplerAnimator.Play(stateName, 0, 0f);
        }

        private void SetFacing(float deltaX)
        {
            if (Mathf.Abs(deltaX) < 0.01f)
            {
                return;
            }

            Vector3 scale = _characterRect.localScale;
            scale.x = Mathf.Abs(scale.x) * (deltaX >= 0f ? 1f : -1f);
            _characterRect.localScale = scale;
        }

        private void SyncVisual()
        {
            if (_samplerRenderer.sprite != null)
            {
                _characterImage.sprite = _samplerRenderer.sprite;
                _characterImage.SetNativeSize();
                LimitCharacterSize();
            }
        }

        private void LimitCharacterSize()
        {
            Vector2 size = _characterRect.sizeDelta;

            if (size.x <= MaxCharacterSize && size.y <= MaxCharacterSize)
            {
                return;
            }

            float scaleFactor = Mathf.Min(MaxCharacterSize / size.x, MaxCharacterSize / size.y);
            _characterRect.sizeDelta = size * scaleFactor;
        }

        private void EnsureOrderedButtonCapacity(int size)
        {
            while (_orderedLevelButtons.Count < size)
            {
                _orderedLevelButtons.Add(null);
            }
        }

        private void EnsureImageListCapacity(int size)
        {
            while (_levelButtonImages.Count < size)
            {
                _levelButtonImages.Add(null);
            }
        }

        private int GetButtonIndex(Button button)
        {
            return _orderedLevelButtons.IndexOf(button);
        }

        private List<Vector2> BuildPathPoints(int targetIndex)
        {
            List<Vector2> points = new();

            if (targetIndex < 0 || targetIndex >= _orderedLevelButtons.Count)
            {
                return points;
            }

            if (_currentLevelIndex < 0)
            {
                for (int index = 0; index <= targetIndex; index++)
                {
                    Button stepButton = _orderedLevelButtons[index];

                    if (stepButton == null)
                    {
                        continue;
                    }

                    points.Add(ConvertTargetToCharacterSpace(stepButton.transform as RectTransform));
                }

                return points;
            }

            if (_currentLevelIndex == targetIndex)
            {
                points.Add(ConvertTargetToCharacterSpace(_orderedLevelButtons[targetIndex].transform as RectTransform));
                return points;
            }

            int direction = targetIndex > _currentLevelIndex ? 1 : -1;

            for (int index = _currentLevelIndex + direction; direction > 0 ? index <= targetIndex : index >= targetIndex; index += direction)
            {
                Button stepButton = _orderedLevelButtons[index];

                if (stepButton == null)
                {
                    continue;
                }

                points.Add(ConvertTargetToCharacterSpace(stepButton.transform as RectTransform));
            }

            return points;
        }
    }
}
