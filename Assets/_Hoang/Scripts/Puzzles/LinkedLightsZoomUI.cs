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

        public bool IsOpen => isPanelOpen;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClicked);
            }

            if (lampButtons != null)
            {
                for (int i = 0; i < lampButtons.Length; i++)
                {
                    int buttonNumber = i + 1;
                    if (lampButtons[i] != null)
                    {
                        lampButtons[i].onClick.AddListener(() => OnLampClicked(buttonNumber));
                    }
                }
            }

            // Đảm bảo panel luôn ẩn khi game bắt đầu
            panelRoot.SetActive(false);
            isPanelOpen = false;
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
                return;
            }

            if (activePuzzle != null)
            {
                activePuzzle.StateChanged -= HandleStateChanged;
            }

            activePuzzle = puzzle;
            onCloseCallback = closeCallback;
            activePuzzle.StateChanged += HandleStateChanged;

            panelRoot.SetActive(true);
            isPanelOpen = true;

            // Mở khóa chuột để người chơi có thể bấm trực tiếp lên 9 ô đèn trên UI
            wasCursorLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateVisuals(activePuzzle.IsReady ? activePuzzle.Mask : -1);
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

            // Trả lại trạng thái khóa chuột ban đầu cho gameplay
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
            if (!isPanelOpen || activePuzzle == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Phím E hoặc Escape đóng màn hình phóng to
            if (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
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
            if (activePuzzle == null || !activePuzzle.IsReady || activePuzzle.IsSolved)
            {
                return;
            }

            activePuzzle.RequestPress(buttonNumber);
        }

        private void OnResetClicked()
        {
            if (activePuzzle == null || !activePuzzle.IsReady || activePuzzle.IsSolved)
            {
                return;
            }

            activePuzzle.RequestReset();
        }

        private void HandleStateChanged(int mask)
        {
            UpdateVisuals(mask);

            // Khi giải xong, tự động thông báo và chuẩn bị đóng
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

