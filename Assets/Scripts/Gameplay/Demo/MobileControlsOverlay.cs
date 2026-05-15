using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SemillasVivas.Gameplay.Demo
{
    
    public sealed class MobileControlsOverlay : MonoBehaviour
    {
        private static readonly string[] BlockingPanelNames =
        {
            "PausePanel",
            "SettingsScreen",
            "SettingsPanel",
            "WinPanel",
            "GameOver",
        };

        private Graphic _linkedJoystickGraphic;
        private bool _inputBlocked;

        private void Awake()
        {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_EDITOR
            Destroy(gameObject);
            return;
#endif
            EnsureEventSystem();
            BuildCanvas();
        }

        private void Update()
        {
            RefreshInputAvailability();
        }

        private void BuildCanvas()
        {
            
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            
        }

        public static void WireExistingJoystick(GameObject joystickGo)
        {
            if (joystickGo == null) return;

            MobileControlsOverlay overlay = FindFirstObjectByType<MobileControlsOverlay>();
            if (overlay == null) return;

            Graphic g = joystickGo.GetComponent<Graphic>();
            if (g == null)
            {
                EmptyGraphic eg = joystickGo.AddComponent<EmptyGraphic>();
                eg.raycastTarget = true;
                g = eg;
            }
            else
            {
                g.raycastTarget = true;
            }

            overlay._linkedJoystickGraphic = g;
        }

        public static void WireExistingButton(GameObject buttonGo, MobileButtonAction action)
        {
            if (buttonGo == null) return;

            if (buttonGo.GetComponent<MobileButton>() != null) return;

            Image existingImage = buttonGo.GetComponentInChildren<Image>(true);
            if (existingImage != null)
            {
                existingImage.raycastTarget = true;
            }
            else
            {
                
                EmptyGraphic eg = buttonGo.AddComponent<EmptyGraphic>();
                eg.raycastTarget = true;
            }

            MobileButton btn = buttonGo.AddComponent<MobileButton>();
            btn.Setup(action);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();

            System.Type inputSystemUIType =
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") ??
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI");

            if (inputSystemUIType != null)
            {
                esGo.AddComponent(inputSystemUIType);
            }
            else
            {
                Debug.LogWarning("[MobileControlsOverlay] InputSystemUIInputModule not found — " +
                                 "falling back to StandaloneInputModule. " +
                                 "Make sure the Input System package is installed.");
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void RefreshInputAvailability()
        {
            bool shouldBlock = IsAnyBlockingPanelOpen();

            if (_inputBlocked == shouldBlock)
            {
                return;
            }

            _inputBlocked = shouldBlock;

            if (_linkedJoystickGraphic != null)
            {
                _linkedJoystickGraphic.raycastTarget = !shouldBlock;
            }

            if (shouldBlock)
            {
                MobileInputState.Horizontal = 0f;
                MobileInputState.Vertical = 0f;
            }
        }

        private static bool IsAnyBlockingPanelOpen()
        {
            for (int index = 0; index < BlockingPanelNames.Length; index++)
            {
                GameObject panel = GameObject.Find(BlockingPanelNames[index]);

                if (panel != null && panel.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
