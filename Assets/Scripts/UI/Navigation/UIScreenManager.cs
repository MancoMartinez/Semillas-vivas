using System;
using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.UI.Navigation
{
    public enum UIScreenId
    {
        Start,
        Loading,
        Menu,
        CharacterSelect,
        LevelSelect,
        Instructions,
        Collection,
        Settings,
        Controls,
    }

    public sealed class UIScreenManager : MonoBehaviour
    {
        private readonly Dictionary<UIScreenId, GameObject> _screens = new();
        private readonly Stack<UIScreenId> _history = new();

        public UIScreenId CurrentScreen { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action<UIScreenId> ScreenChanged;

        public void Register(UIScreenId screenId, GameObject screenRoot)
        {
            if (screenRoot == null)
            {
                throw new ArgumentNullException(nameof(screenRoot));
            }

            _screens[screenId] = screenRoot;
        }

        public void Initialize(UIScreenId initialScreen)
        {
            if (_screens.Count == 0)
            {
                Debug.LogWarning("UIScreenManager initialized without registered screens.");
                return;
            }

            foreach (KeyValuePair<UIScreenId, GameObject> entry in _screens)
            {
                entry.Value.SetActive(false);
            }

            _history.Clear();
            IsInitialized = true;
            Show(initialScreen, false);
        }

        public void Show(UIScreenId screenId, bool rememberCurrentScreen = true)
        {
            if (!TryGetScreen(screenId, out GameObject nextScreen))
            {
                Debug.LogWarning($"Screen '{screenId}' is not registered.");
                return;
            }

            if (IsInitialized && rememberCurrentScreen && _screens.ContainsKey(CurrentScreen) && CurrentScreen != screenId)
            {
                _history.Push(CurrentScreen);
            }

            foreach (KeyValuePair<UIScreenId, GameObject> entry in _screens)
            {
                entry.Value.SetActive(entry.Key == screenId);
            }

            CurrentScreen = screenId;
            ScreenChanged?.Invoke(screenId);
        }

        public void GoBack()
        {
            if (_history.Count == 0)
            {
                return;
            }

            UIScreenId previousScreen = _history.Pop();
            Show(previousScreen, false);
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        public bool TryGetScreen(UIScreenId screenId, out GameObject screenRoot)
        {
            return _screens.TryGetValue(screenId, out screenRoot);
        }
    }
}
