using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hoang.Puzzles
{
    [RequireComponent(typeof(Collider))]
    public sealed class SymbolCipherConsole : MonoBehaviour
    {
        private static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        [SerializeField] private SymbolCipherPuzzle _puzzle;
        [SerializeField] private TMP_Text _displayText;
        private readonly HashSet<Collider> _localPlayers = new HashSet<Collider>();
        private string _entry = "";
        private bool _editing;
        private bool _waiting;
        private float _responseDeadline;

        private void Awake()
        {
            if (_puzzle == null || _displayText == null)
            {
                Debug.LogError("[SymbolCipherConsole] Cần gán puzzle và bảng hiển thị.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_puzzle == null || _displayText == null) return;
            _puzzle.SubmissionCompleted += HandleSubmission;
            _puzzle.SolvedChanged += HandleSolved;
            ShowIdle();
        }

        private void OnDisable()
        {
            if (_puzzle != null)
            {
                _puzzle.SubmissionCompleted -= HandleSubmission;
                _puzzle.SolvedChanged -= HandleSolved;
            }
            _localPlayers.Clear();
            _editing = false;
            _waiting = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_puzzle == null) return;
            NetworkObject player = other.GetComponentInParent<NetworkObject>();
            if (_puzzle.IsNetworkSessionActive)
            {
                if (player == null || !player.IsSpawned || !player.IsPlayerObject || !player.IsOwner) return;
            }
            else if (other.GetComponentInParent<CharacterController>() == null && !other.CompareTag("Player")) return;
            _localPlayers.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_localPlayers.Remove(other) && _localPlayers.Count == 0) EndInput();
        }

        private void Update()
        {
            _localPlayers.RemoveWhere(other => other == null || !other.enabled || !other.gameObject.activeInHierarchy);
            if (!_puzzle.IsReady || _puzzle.IsSolved || !Application.isFocused || _localPlayers.Count == 0)
            {
                if (_editing || _waiting) EndInput();
                return;
            }

            Keyboard keys = Keyboard.current;
            if (keys == null) return;
            if (keys.eKey.wasPressedThisFrame)
            {
                if (_editing) EndInput();
                else { _editing = true; _entry = ""; ShowEntry(); }
                return;
            }
            if (!_editing) return;
            if (_waiting)
            {
                if (Time.unscaledTime >= _responseDeadline)
                {
                    _waiting = false;
                    _displayText.text = "Chưa nhận được phản hồi.\nEnter để thử lại / E để thoát.";
                }
                return;
            }

            bool changed = false;
            for (int digit = 0; digit < DigitKeys.Length && _entry.Length < 3; digit++)
            {
                if (keys[DigitKeys[digit]].wasPressedThisFrame || keys[NumpadKeys[digit]].wasPressedThisFrame)
                {
                    _entry += (char)('0' + digit);
                    changed = true;
                }
            }
            if (keys.backspaceKey.wasPressedThisFrame && _entry.Length > 0)
            {
                _entry = _entry.Substring(0, _entry.Length - 1);
                changed = true;
            }
            if (changed) ShowEntry();
            if (!keys.enterKey.wasPressedThisFrame && !keys.numpadEnterKey.wasPressedThisFrame) return;
            if (_entry.Length != 3)
            {
                _displayText.text = "Cần nhập đủ 3 chữ số.\nBackspace: xóa / E: thoát";
                return;
            }
            _waiting = true;
            _responseDeadline = Time.unscaledTime + 5f;
            _displayText.text = "Đang kiểm tra mã...";
            _puzzle.SubmitCode(_entry);
        }

        private void HandleSubmission(bool accepted)
        {
            _waiting = false;
            if (accepted) { EndInput(); return; }
            if (!_editing) return;
            _entry = "";
            _displayText.text = "Mã chưa đúng hoặc chưa thể tương tác.\nHãy đọc lại gợi ý và thử lại.\n\n_ _ _\nE: thoát";
        }

        private void HandleSolved(bool solved)
        {
            EndInput();
        }

        private void EndInput()
        {
            _editing = false;
            _waiting = false;
            _entry = "";
            ShowIdle();
        }

        private void ShowEntry()
        {
            _displayText.text = "MÃ: " + _entry.PadRight(3, '_') +
                "\n\n0–9: nhập / Enter: xác nhận\nBackspace: xóa / E: thoát";
        }

        private void ShowIdle()
        {
            if (_displayText == null) return;
            _displayText.text = _puzzle != null && _puzzle.IsSolved
                ? "ĐÃ GIẢI MÃ\nLỐI ĐI ĐÃ MỞ"
                : "BÀN NHẬP MÃ\n\nĐứng gần và nhấn E\nđể nhập 3 chữ số.";
        }
    }
}
