using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hoang.Puzzles
{
    public sealed class LinkedLightsZoomUI : MonoBehaviour
    {
        private static readonly Key[] digitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] numpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5,
            Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header & Info")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text rulesText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button[] lampButtons;
        [SerializeField] private Image[] lampImages;
        [SerializeField] private TMP_Text[] lampNumbers;

        [Header("Visual Colors")]
        [SerializeField] private Color litColor = new Color(1f, 0.70f, 0.18f, 1f);
        [SerializeField] private Color darkColor = new Color(0.12f, 0.16f, 0.22f, 1f);
        [SerializeField] private Color litTextColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color darkTextColor = new Color(0.85f, 0.90f, 0.95f, 1f);

        private LinkedLightsPuzzle activePuzzle;
        private Action onCloseCallback;
        private bool isPanelOpen;
        private bool wasCursorLocked;
        private int openFrame = -1;
        private bool listenersBound;

        public bool IsOpen => isPanelOpen;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            SetupListeners();
            SanitizeRaycastsAndLayers();

            // Chỉ tắt panel nếu không phải đang được mở thông qua hàm Open
            if (!isPanelOpen)
            {
                panelRoot.SetActive(false);
            }
        }

        private void SetupListeners()
        {
            if (listenersBound) return;
            listenersBound = true;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(OnResetClicked);
            }

            if (exitButton == null && panelRoot != null)
            {
                Transform exitTrans = panelRoot.transform.Find("Exit_Button");
                if (exitTrans != null) exitButton = exitTrans.GetComponent<Button>();
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(Close);
            }

            if (lampButtons != null)
            {
                for (int i = 0; i < lampButtons.Length; i++)
                {
                    int buttonNumber = i + 1;
                    if (lampButtons[i] != null)
                    {
                        lampButtons[i].onClick.RemoveAllListeners();
                        lampButtons[i].onClick.AddListener(() => OnLampClicked(buttonNumber));
                    }
                }
            }
        }

        private void SanitizeRaycastsAndLayers()
        {
            // Thiết lập toàn bộ UI panel sang Layer 5 (UI) để đảm bảo chuẩn GraphicRaycaster của Unity
            SetLayerRecursively(panelRoot != null ? panelRoot : gameObject, 5);

            // Tắt raycastTarget trên các text con bên trong nút để click chuột xuyên thẳng vào component Button
            if (lampNumbers != null)
            {
                foreach (TMP_Text label in lampNumbers)
                {
                    if (label != null) label.raycastTarget = false;
                }
            }

            if (titleText != null) titleText.raycastTarget = false;
            if (statusText != null) statusText.raycastTarget = false;
            if (rulesText != null) rulesText.raycastTarget = false;

            // Tắt navigation mode trên tất cả buttons để tránh bàn phím bị cướp tiêu điểm focus
            if (lampButtons != null)
            {
                foreach (Button btn in lampButtons)
                {
                    if (btn != null)
                    {
                        Navigation nav = btn.navigation;
                        nav.mode = Navigation.Mode.None;
                        btn.navigation = nav;
                    }
                }
            }

            if (closeButton != null)
            {
                Navigation nav = closeButton.navigation;
                nav.mode = Navigation.Mode.None;
                closeButton.navigation = nav;
            }
            if (resetButton != null)
            {
                Navigation nav = resetButton.navigation;
                nav.mode = Navigation.Mode.None;
                resetButton.navigation = nav;
            }
            if (exitButton != null)
            {
                Navigation nav = exitButton.navigation;
                nav.mode = Navigation.Mode.None;
                exitButton.navigation = nav;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null) return;
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void OnDestroy()
        {
            if (activePuzzle != null)
            {
                activePuzzle.StateChanged -= HandleStateChanged;
            }
        }

        public void Open(LinkedLightsPuzzle puzzle, Action closeCallback)
        {
            if (puzzle == null)
            {
                puzzle = FindFirstObjectByType<LinkedLightsPuzzle>();
            }

            if (activePuzzle != null)
            {
                activePuzzle.StateChanged -= HandleStateChanged;
            }

            activePuzzle = puzzle;
            onCloseCallback = closeCallback;
            if (activePuzzle != null)
            {
                activePuzzle.StateChanged += HandleStateChanged;
            }

            isPanelOpen = true;
            openFrame = Time.frameCount;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            SetupListeners();
            SanitizeRaycastsAndLayers();

            wasCursorLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int currentMask = activePuzzle != null && activePuzzle.IsReady ? activePuzzle.Mask : -1;
            UpdateVisuals(currentMask);
        }

        public void Close()
        {
            if (!isPanelOpen)
            {
                return;
            }

            isPanelOpen = false;

            if (activePuzzle != null)
            {
                activePuzzle.StateChanged -= HandleStateChanged;
                activePuzzle = null;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (wasCursorLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Action callback = onCloseCallback;
            onCloseCallback = null;
            callback?.Invoke();
        }

        private void Update()
        {
            if (!isPanelOpen)
            {
                return;
            }

            // Tự động liên kết puzzle nếu bị mất tham chiếu khi mở độc lập trong editor
            if (activePuzzle == null)
            {
                activePuzzle = FindFirstObjectByType<LinkedLightsPuzzle>();
                if (activePuzzle != null)
                {
                    activePuzzle.StateChanged -= HandleStateChanged;
                    activePuzzle.StateChanged += HandleStateChanged;
                    UpdateVisuals(activePuzzle.IsReady ? activePuzzle.Mask : -1);
                }
            }

            // Luôn đảm bảo chuột hiển thị và không bị khóa trong suốt thời gian màn hình phóng to đang mở
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Bỏ qua cùng frame khi bấm E để mở, tránh việc vừa mở xong bị lệnh E đóng ngay lập tức
            if (Time.frameCount > openFrame && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            // Phím R để đặt lại câu đố
            if (keyboard.rKey.wasPressedThisFrame)
            {
                OnResetClicked();
                return;
            }

            // Phím số 1 đến 9 để thao tác nhanh ô đèn
            for (int i = 0; i < 9; i++)
            {
                if (keyboard[digitKeys[i]].wasPressedThisFrame || keyboard[numpadKeys[i]].wasPressedThisFrame)
                {
                    OnLampClicked(i + 1);
                    return;
                }
            }
        }

        private void OnLampClicked(int buttonNumber)
        {
            if (activePuzzle == null)
            {
                activePuzzle = FindFirstObjectByType<LinkedLightsPuzzle>();
            }

            if (activePuzzle == null || !activePuzzle.IsReady || activePuzzle.IsSolved)
            {
                return;
            }

            activePuzzle.RequestPress(buttonNumber);
        }

        private void OnResetClicked()
        {
            if (activePuzzle == null)
            {
                activePuzzle = FindFirstObjectByType<LinkedLightsPuzzle>();
            }

            if (activePuzzle == null || !activePuzzle.IsReady || activePuzzle.IsSolved)
            {
                return;
            }

            activePuzzle.RequestReset();
        }

        private void HandleStateChanged(int mask)
        {
            UpdateVisuals(mask);

            if (activePuzzle != null && activePuzzle.IsSolved)
            {
                if (statusText != null)
                {
                    statusText.text = "<color=#40FF70>XUẤT SẮC! ĐÃ TẮT HẾT ĐÈN - CỔNG ĐÃ MỞ!</color>";
                }
            }
        }

        private void UpdateVisuals(int mask)
        {
            if (lampImages == null)
            {
                return;
            }

            for (int i = 0; i < lampImages.Length && i < 9; i++)
            {
                bool isLit = mask >= 0 && (mask & (1 << i)) != 0;
                if (lampImages[i] != null)
                {
                    lampImages[i].color = isLit ? litColor : darkColor;
                }

                if (lampNumbers != null && i < lampNumbers.Length && lampNumbers[i] != null)
                {
                    lampNumbers[i].color = isLit ? litTextColor : darkTextColor;
                }
            }

            if (statusText != null)
            {
                if (activePuzzle == null || !activePuzzle.IsReady)
                {
                    statusText.text = "Đang kết nối câu đố...";
                }
                else if (activePuzzle.IsSolved)
                {
                    statusText.text = "<color=#40FF70>XUẤT SẮC! ĐÃ TẮT HẾT ĐÈN - CỔNG ĐÃ MỞ!</color>";
                }
                else
                {
                    statusText.text = "Bấm trực tiếp vào ô số (hoặc nhấn phím 1–9) | R: Đặt lại | E: Thoát";
                }
            }
        }
    }
}
