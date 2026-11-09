using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hoang.Puzzles
{
    public sealed class SymbolCipherZoomUI : MonoBehaviour
    {
        private static readonly Key[] digitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] numpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header & Clues")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text cluesText;
        [SerializeField] private TMP_Text codeDisplayText;
        [SerializeField] private TMP_Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button backspaceButton;
        [SerializeField] private Button clearButton;
        [Tooltip("Mảng đúng 10 button tương ứng chữ số từ 0 đến 9")]
        [SerializeField] private Button[] digitButtons;

        private SymbolCipherPuzzle activePuzzle;
        private Action onCloseCallback;
        private string currentEntry = "";
        private bool isPanelOpen;
        private bool wasCursorLocked;
        private bool isWaitingResponse;
        private Coroutine autoCloseRoutine;
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

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (backspaceButton != null)
            {
                backspaceButton.onClick.RemoveAllListeners();
                backspaceButton.onClick.AddListener(OnBackspaceClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(OnClearClicked);
            }

            if (digitButtons != null)
            {
                for (int i = 0; i < digitButtons.Length && i < 10; i++)
                {
                    char digitChar = (char)('0' + i);
                    if (digitButtons[i] != null)
                    {
                        digitButtons[i].onClick.RemoveAllListeners();
                        digitButtons[i].onClick.AddListener(() => OnDigitClicked(digitChar));
                    }
                }
            }
        }

        private void SanitizeRaycastsAndLayers()
        {
            SetLayerRecursively(panelRoot != null ? panelRoot : gameObject, 5);

            if (digitButtons != null)
            {
                foreach (Button btn in digitButtons)
                {
                    if (btn != null)
                    {
                        Navigation nav = btn.navigation;
                        nav.mode = Navigation.Mode.None;
                        btn.navigation = nav;

                        TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                        if (label != null) label.raycastTarget = false;
                    }
                }
            }

            DisableChildTextRaycast(closeButton);
            DisableChildTextRaycast(submitButton);
            DisableChildTextRaycast(backspaceButton);
            DisableChildTextRaycast(clearButton);

            if (titleText != null) titleText.raycastTarget = false;
            if (cluesText != null) cluesText.raycastTarget = false;
            if (codeDisplayText != null) codeDisplayText.raycastTarget = false;
            if (statusText != null) statusText.raycastTarget = false;
        }

        private static void DisableChildTextRaycast(Button button)
        {
            if (button == null) return;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.raycastTarget = false;
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
                activePuzzle.SubmissionCompleted -= HandleSubmission;
                activePuzzle.SolvedChanged -= HandleSolved;
            }
        }

        public void Open(SymbolCipherPuzzle puzzle, Action closeCallback)
        {
            if (puzzle == null)
            {
                puzzle = FindFirstObjectByType<SymbolCipherPuzzle>();
            }

            if (activePuzzle != null)
            {
                activePuzzle.SubmissionCompleted -= HandleSubmission;
                activePuzzle.SolvedChanged -= HandleSolved;
            }

            activePuzzle = puzzle;
            onCloseCallback = closeCallback;
            currentEntry = "";
            isWaitingResponse = false;

            if (activePuzzle != null)
            {
                activePuzzle.SubmissionCompleted += HandleSubmission;
                activePuzzle.SolvedChanged += HandleSolved;
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

            UpdateDisplay();
            if (statusText != null)
            {
                statusText.text = activePuzzle != null && activePuzzle.IsSolved
                    ? "<color=#40FF70>CỔNG ĐÁ ĐÃ ĐƯỢC MỞ KHÓA!</color>"
                    : "Nhập 3 chữ số theo thứ tự: ●  ▲  ■";
            }
        }

        public void Close()
        {
            if (!isPanelOpen)
            {
                return;
            }

            if (autoCloseRoutine != null)
            {
                StopCoroutine(autoCloseRoutine);
                autoCloseRoutine = null;
            }

            isPanelOpen = false;

            if (activePuzzle != null)
            {
                activePuzzle.SubmissionCompleted -= HandleSubmission;
                activePuzzle.SolvedChanged -= HandleSolved;
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

            if (activePuzzle == null)
            {
                activePuzzle = FindFirstObjectByType<SymbolCipherPuzzle>();
                if (activePuzzle != null)
                {
                    activePuzzle.SubmissionCompleted -= HandleSubmission;
                    activePuzzle.SolvedChanged -= HandleSolved;
                    activePuzzle.SubmissionCompleted += HandleSubmission;
                    activePuzzle.SolvedChanged += HandleSolved;
                }
            }

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

            if (Time.frameCount > openFrame && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            if (isWaitingResponse || (activePuzzle != null && activePuzzle.IsSolved))
            {
                return;
            }

            // Phím số 0–9
            for (int i = 0; i < 10; i++)
            {
                if (keyboard[digitKeys[i]].wasPressedThisFrame || keyboard[numpadKeys[i]].wasPressedThisFrame)
                {
                    OnDigitClicked((char)('0' + i));
                    return;
                }
            }

            // Phím Backspace để xóa
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                OnBackspaceClicked();
                return;
            }

            // Phím Enter để gửi mã
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                OnSubmitClicked();
            }
        }

        private void OnDigitClicked(char digit)
        {
            if (isWaitingResponse || activePuzzle == null || activePuzzle.IsSolved)
            {
                return;
            }

            if (currentEntry.Length < 3)
            {
                currentEntry += digit;
                UpdateDisplay();
            }
        }

        private void OnBackspaceClicked()
        {
            if (isWaitingResponse || activePuzzle == null || activePuzzle.IsSolved)
            {
                return;
            }

            if (currentEntry.Length > 0)
            {
                currentEntry = currentEntry.Substring(0, currentEntry.Length - 1);
                UpdateDisplay();
            }
        }

        private void OnClearClicked()
        {
            if (isWaitingResponse || activePuzzle == null || activePuzzle.IsSolved)
            {
                return;
            }

            currentEntry = "";
            UpdateDisplay();
        }

        private void OnSubmitClicked()
        {
            if (isWaitingResponse || activePuzzle == null || activePuzzle.IsSolved)
            {
                return;
            }

            if (currentEntry.Length != 3)
            {
                if (statusText != null)
                {
                    statusText.text = "<color=#FFB020>Cần nhập đủ 3 chữ số trước khi xác nhận!</color>";
                }
                return;
            }

            isWaitingResponse = true;
            if (statusText != null)
            {
                statusText.text = "Đang kiểm tra mã số...";
            }

            activePuzzle.SubmitCode(currentEntry);
        }

        private void HandleSubmission(bool accepted)
        {
            isWaitingResponse = false;
            if (accepted)
            {
                if (statusText != null)
                {
                    statusText.text = "<color=#40FF70>CHÍNH XÁC! CỔNG ĐÁ ĐANG MỞ...</color>";
                }
                autoCloseRoutine = StartCoroutine(AutoCloseAfterDelay(1.5f));
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "<color=#FF5555>MÃ CHƯA ĐÚNG! Hãy tính lại theo gợi ý.</color>";
                }
                currentEntry = "";
                UpdateDisplay();
            }
        }

        private void HandleSolved(bool solved)
        {
            if (solved)
            {
                if (statusText != null)
                {
                    statusText.text = "<color=#40FF70>CỔNG ĐÁ ĐÃ MỞ THÀNH CÔNG!</color>";
                }
            }
        }

        private IEnumerator AutoCloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Close();
        }

        private void UpdateDisplay()
        {
            if (codeDisplayText == null)
            {
                return;
            }

            char c1 = currentEntry.Length > 0 ? currentEntry[0] : '_';
            char c2 = currentEntry.Length > 1 ? currentEntry[1] : '_';
            char c3 = currentEntry.Length > 2 ? currentEntry[2] : '_';

            codeDisplayText.text = $"[  {c1}  ]   [  {c2}  ]   [  {c3}  ]";
        }
    }
}
