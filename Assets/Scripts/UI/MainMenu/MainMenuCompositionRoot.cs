using System.Collections;
using SemillasVivas.UI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    public sealed class MainMenuCompositionRoot : MonoBehaviour
    {
        [SerializeField] private float loadingScreenDuration = 0.75f;
        [SerializeField] private RuntimeAnimatorController levelSelectCharacterAnimatorController;

        private UIScreenManager _screenManager;
        private CollectionScreenController _collectionController;
        private CharacterSelectController _characterSelectController;
        private LevelSelectScreenController _levelSelectScreenController;
        private SettingsScreenController _settingsScreenController;
        private Coroutine _loadingRoutine;

        private void Awake()
        {
            _screenManager = gameObject.GetComponent<UIScreenManager>();

            if (_screenManager == null)
            {
                _screenManager = gameObject.AddComponent<UIScreenManager>();
            }

            Transform screenContainer = UIPathUtility.FindRequired(transform, "Screen_Container");

            RegisterScreens(screenContainer);
            _screenManager.Initialize(UIScreenId.Start);

            _collectionController = new CollectionScreenController();
            _collectionController.Initialize(UIPathUtility.FindRequired(screenContainer, "CollectionScreen"));

            _characterSelectController = new CharacterSelectController();
            _characterSelectController.Initialize(
                UIPathUtility.FindRequired(screenContainer, "CharacterSelectScreen"),
                () => _screenManager.Show(UIScreenId.LevelSelect));

            _levelSelectScreenController = new LevelSelectScreenController();
            _levelSelectScreenController.Initialize(
                this,
                UIPathUtility.FindRequired(screenContainer, "LevelSelectScreen"),
                levelSelectCharacterAnimatorController,
                TryLoadLevel);

            _settingsScreenController = new SettingsScreenController();
            _settingsScreenController.Initialize(UIPathUtility.FindRequired(screenContainer, "SettingsScreen"));

            BindNavigation(screenContainer);
        }

        private void RegisterScreens(Transform screenContainer)
        {
            _screenManager.Register(UIScreenId.Start, UIPathUtility.FindRequired(screenContainer, "StartScreen").gameObject);
            _screenManager.Register(UIScreenId.Loading, UIPathUtility.FindRequired(screenContainer, "LoadingScreen").gameObject);
            _screenManager.Register(UIScreenId.Menu, UIPathUtility.FindRequired(screenContainer, "MenuScreen").gameObject);
            _screenManager.Register(UIScreenId.CharacterSelect, UIPathUtility.FindRequired(screenContainer, "CharacterSelectScreen").gameObject);
            _screenManager.Register(UIScreenId.LevelSelect, UIPathUtility.FindRequired(screenContainer, "LevelSelectScreen").gameObject);
            _screenManager.Register(UIScreenId.Instructions, UIPathUtility.FindRequired(screenContainer, "Instructions").gameObject);
            _screenManager.Register(UIScreenId.Collection, UIPathUtility.FindRequired(screenContainer, "CollectionScreen").gameObject);
            _screenManager.Register(UIScreenId.Settings, UIPathUtility.FindRequired(screenContainer, "SettingsScreen").gameObject);
            _screenManager.Register(UIScreenId.Controls, UIPathUtility.FindRequired(screenContainer, "ControlsScreen").gameObject);
        }

        private void BindNavigation(Transform screenContainer)
        {
            Bind(screenContainer, "StartScreen/Start", StartGameFlow);
            Bind(screenContainer, "StartScreen/Close", ExitApplication);

            Bind(screenContainer, "MenuScreen/Back", () => _screenManager.Show(UIScreenId.Start));
            Bind(screenContainer, "MenuScreen/Character", () => _screenManager.Show(UIScreenId.Collection));
            Bind(screenContainer, "MenuScreen/Buttons_Right/Play", () => _screenManager.Show(UIScreenId.CharacterSelect));
            Bind(screenContainer, "MenuScreen/Buttons_Right/Levels", () => _screenManager.Show(UIScreenId.LevelSelect));
            Bind(screenContainer, "MenuScreen/Buttons_Right/Instructions", () => _screenManager.Show(UIScreenId.Instructions));
            Bind(screenContainer, "MenuScreen/Buttons_Right/Settings", () => _screenManager.Show(UIScreenId.Settings));
            Bind(screenContainer, "MenuScreen/Buttons_Right/Exit", ExitApplication);

            Bind(screenContainer, "CharacterSelectScreen/Back", () => _screenManager.Show(UIScreenId.Menu));

            Bind(screenContainer, "LevelSelectScreen/Back", () => _screenManager.Show(UIScreenId.Menu));
            _levelSelectScreenController.BindLevelButton(UIPathUtility.EnsureButton(screenContainer, "LevelSelectScreen/Levels/Lvl_1"), "Level_01", 0);
            _levelSelectScreenController.BindLevelButton(UIPathUtility.EnsureButton(screenContainer, "LevelSelectScreen/Levels/Lvl_2"), "Level_02", 1);
            _levelSelectScreenController.BindLevelButton(UIPathUtility.EnsureButton(screenContainer, "LevelSelectScreen/Levels/Lvl_3"), "Level_03", 2);
            _levelSelectScreenController.BindLevelButton(UIPathUtility.EnsureButton(screenContainer, "LevelSelectScreen/Levels/Lvl_4"), "Level_04", 3);
            _levelSelectScreenController.BindLevelButton(UIPathUtility.EnsureButton(screenContainer, "LevelSelectScreen/Levels/Lvl_5"), "Level_05", 4);

            Bind(screenContainer, "Instructions/Back", () => _screenManager.Show(UIScreenId.Menu));

            Bind(screenContainer, "CollectionScreen/Back", HandleCollectionBack);

            Bind(screenContainer, "SettingsScreen/Back", () => _screenManager.Show(UIScreenId.Menu));
            Bind(screenContainer, "SettingsScreen/Background/Game/ButtonOptions/Controls", () => _screenManager.Show(UIScreenId.Controls));

            Bind(screenContainer, "ControlsScreen/Back", () => _screenManager.Show(UIScreenId.Settings));
        }

        private void Update()
        {
            _levelSelectScreenController?.Tick();
        }

        private void OnDestroy()
        {
            _levelSelectScreenController?.Dispose();
        }

        private void Bind(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            Button button = UIPathUtility.EnsureButton(root, path);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void StartGameFlow()
        {
            if (_loadingRoutine != null)
            {
                StopCoroutine(_loadingRoutine);
            }

            _loadingRoutine = StartCoroutine(ShowLoadingThenMenu());
        }

        private IEnumerator ShowLoadingThenMenu()
        {
            _screenManager.ClearHistory();
            _screenManager.Show(UIScreenId.Loading, false);

            yield return new WaitForSeconds(loadingScreenDuration);

            _screenManager.Show(UIScreenId.Menu, false);
            _loadingRoutine = null;
        }

        private void HandleCollectionBack()
        {
            if (_collectionController.HandleBackRequested())
            {
                return;
            }

            _screenManager.Show(UIScreenId.Menu);
        }

        private void TryLoadLevel(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            Debug.LogWarning($"Scene '{sceneName}' is not available in Build Settings yet.");
        }

        private static void ExitApplication()
        {
            Application.Quit();
            Debug.Log("Exit requested from main menu.");
        }
    }

    internal static class UIPathUtility
    {
        public static Transform FindRequired(Transform root, string path)
        {
            Transform target = root.Find(path);

            if (target == null)
            {
                throw new MissingReferenceException($"UI path not found: {path}");
            }

            return target;
        }

        public static Button EnsureButton(Transform root, string path)
        {
            return EnsureButton(FindRequired(root, path));
        }

        public static Button EnsureButton(Transform target)
        {
            Button button = target.GetComponent<Button>();

            if (button == null)
            {
                button = target.gameObject.AddComponent<Button>();
            }

            if (button.targetGraphic == null)
            {
                Graphic graphic = target.GetComponent<Graphic>();

                if (graphic == null)
                {
                    graphic = target.GetComponentInChildren<Graphic>(true);
                }

                button.targetGraphic = graphic;
            }

            return button;
        }
    }
}
