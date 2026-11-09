using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Hoang.Puzzles
{
    [RequireComponent(typeof(Collider))]
    public sealed class SymbolCipherConsole : MonoBehaviour
    {
        private static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        [FormerlySerializedAs("_puzzle")]
        [SerializeField] private SymbolCipherPuzzle puzzle;

        [FormerlySerializedAs("_displayText")]
        [SerializeField] private TMP_Text displayText;

        [Tooltip("Màn hình UI phóng to (kích thước bằng Setting Panel) khi nhấn E tương tác.")]
        [SerializeField] private SymbolCipherZoomUI zoomUI;

        private readonly HashSet<Collider> localPlayers = new HashSet<Collider>();
        private string entry = "";
        private bool isEditing;
        private bool isWaiting;
        private float responseDeadline;

        private void Awake()
        {
            if (puzzle == null || displayText == null)
            {
                Debug.LogError("[SymbolCipherConsole] Cần gán puzzle và bảng hiển thị.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (puzzle == null || displayText == null) return;
            puzzle.SubmissionCompleted += HandleSubmission;
            puzzle.SolvedChanged += HandleSolved;
            ShowIdle();
        }

        private void OnDisable()
        {
            if (puzzle != null)
            {
                puzzle.SubmissionCompleted -= HandleSubmission;
                puzzle.SolvedChanged -= HandleSolved;
            }
            localPlayers.Clear();
            EndInput();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (puzzle == null) return;
            NetworkObject player = other.GetComponentInParent<NetworkObject>();
            if (puzzle.IsNetworkSessionActive)
            {
                if (player == null || !player.IsSpawned || !player.IsPlayerObject || !player.IsOwner) return;
            }
            else if (other.GetComponentInParent<CharacterController>() == null && !other.CompareTag("Player")) return;
            localPlayers.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (localPlayers.Remove(other) && localPlayers.Count == 0) EndInput();
        }

        private void Update()
        {
            localPlayers.RemoveWhere(other => other == null || !other.enabled || !other.gameObject.activeInHierarchy);
            if (!puzzle.IsReady || puzzle.IsSolved || !Application.isFocused || localPlayers.Count == 0)
            {
                if (isEditing || isWaiting) EndInput();
                return;
            }

            // Khi màn hình phóng to đang mở, nhường toàn bộ quyền xử lý phím và chuột cho ZoomUI
            if (zoomUI != null && zoomUI.IsOpen)
            {
                return;
            }

            Keyboard keys = Keyboard.current;
            if (keys == null) return;

            // Nhấn E để bật / tắt nhập mã và mở màn hình phóng to
            if (keys.eKey.wasPressedThisFrame)
            {
                if (isEditing)
                {
                    EndInput();
                }
                else
                {
                    StartInput();
                }
                return;
            }

            if (!isEditing) return;

            if (isWaiting)
            {
                if (Time.unscaledTime >= responseDeadline)
                {
                    isWaiting = false;
                    displayText.text = "Chưa nhận được phản hồi.\nEnter để thử lại / E để thoát.";
                }
                return;
            }

            bool changed = false;
            for (int digit = 0; digit < DigitKeys.Length && entry.Length < 3; digit++)
            {
                if (keys[DigitKeys[digit]].wasPressedThisFrame || keys[NumpadKeys[digit]].wasPressedThisFrame)
                {
                    entry += (char)('0' + digit);
                    changed = true;
                }
            }

            if (keys.backspaceKey.wasPressedThisFrame && entry.Length > 0)
            {
                entry = entry.Substring(0, entry.Length - 1);
                changed = true;
            }

            if (changed) ShowEntry();

            if (!keys.enterKey.wasPressedThisFrame && !keys.numpadEnterKey.wasPressedThisFrame) return;

            if (entry.Length != 3)
            {
                displayText.text = "Cần nhập đủ 3 chữ số.\nBackspace: xóa / E: thoát";
                return;
            }

            isWaiting = true;
            responseDeadline = Time.unscaledTime + 5f;
            displayText.text = "Đang kiểm tra mã...";
            puzzle.SubmitCode(entry);
        }

        private void StartInput()
        {
            isEditing = true;
            entry = "";
            ShowEntry();
            EnsureZoomUI();
            if (zoomUI != null)
            {
                zoomUI.Open(puzzle, () =>
                {
                    isEditing = false;
                    isWaiting = false;
                    entry = "";
                    ShowIdle();
                });
            }
        }

        private void EnsureZoomUI()
        {
            if (zoomUI == null)
            {
                zoomUI = FindFirstObjectByType<SymbolCipherZoomUI>(FindObjectsInactive.Include);
            }
        }

        private void HandleSubmission(bool accepted)
        {
            isWaiting = false;
            if (accepted)
            {
                EndInput();
                return;
            }

            if (!isEditing) return;
            entry = "";
            displayText.text = "Mã chưa đúng hoặc chưa thể tương tác.\nHãy đọc lại gợi ý và thử lại.\n\n_ _ _\nE: thoát";
        }

        private void HandleSolved(bool solved)
        {
            EndInput();
        }

        private void EndInput()
        {
            isEditing = false;
            isWaiting = false;
            entry = "";
            if (zoomUI != null && zoomUI.IsOpen)
            {
                zoomUI.Close();
            }
            ShowIdle();
        }

        private void ShowEntry()
        {
            displayText.text = "MÃ: " + entry.PadRight(3, '_') +
                "\n\n0–9: nhập / Enter: xác nhận\nBackspace: xóa / E: thoát";
        }

        private void ShowIdle()
        {
            if (displayText == null) return;
            displayText.text = puzzle != null && puzzle.IsSolved
                ? "ĐÃ GIẢI MÃ\nLỐI ĐI ĐÃ MỞ"
                : "BÀN NHẬP MÃ\n\nĐứng gần và nhấn E\nđể nhập 3 chữ số.";
        }
    }
}
