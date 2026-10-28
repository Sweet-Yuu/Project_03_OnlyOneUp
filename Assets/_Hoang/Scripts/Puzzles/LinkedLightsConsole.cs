using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hoang.Puzzles
{
    [RequireComponent(typeof(Collider))]
    public sealed class LinkedLightsConsole : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5,
            Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
        private static readonly Color LitColor = new Color(1f, 0.68f, 0.16f);
        private static readonly Color DarkColor = new Color(0.035f, 0.055f, 0.08f);

        [SerializeField] private LinkedLightsPuzzle _puzzle;
        [SerializeField] private TMP_Text _displayText;
        [Tooltip("Đúng chín renderer, thứ tự trái sang phải, trên xuống dưới.")]
        [SerializeField] private MeshRenderer[] _lamps;
        private readonly HashSet<Collider> _localPlayers = new HashSet<Collider>();
        private MaterialPropertyBlock _properties;
        private bool _editing;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            bool valid = _puzzle != null && _displayText != null && _lamps != null && _lamps.Length == 9;
            if (valid)
                foreach (MeshRenderer lamp in _lamps) valid &= lamp != null;
            if (!valid)
            {
                Debug.LogError("[LinkedLightsConsole] Cần puzzle, bảng trạng thái và đúng chín đèn theo thứ tự 1–9.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_puzzle == null || _properties == null || _displayText == null || _lamps == null || _lamps.Length != 9) return;
            _puzzle.StateChanged += HandleStateChanged;
            HandleStateChanged(_puzzle.IsReady ? _puzzle.Mask : -1);
        }

        private void OnDisable()
        {
            if (_puzzle != null) _puzzle.StateChanged -= HandleStateChanged;
            _localPlayers.Clear();
            _editing = false;
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
                if (_editing) EndInput();
                return;
            }
            Keyboard keys = Keyboard.current;
            if (keys == null) return;
            if (keys.eKey.wasPressedThisFrame)
            {
                _editing = !_editing;
                RefreshPrompt();
                return;
            }
            if (!_editing) return;
            if (keys.rKey.wasPressedThisFrame) { _puzzle.RequestReset(); return; }
            for (int index = 0; index < 9; index++)
                if (keys[DigitKeys[index]].wasPressedThisFrame || keys[NumpadKeys[index]].wasPressedThisFrame)
                    _puzzle.RequestPress(index + 1);
        }

        private void HandleStateChanged(int mask)
        {
            if (mask < 0 || mask == 0) _editing = false;
            for (int index = 0; index < 9; index++)
            {
                _properties.Clear();
                _properties.SetColor(BaseColor, mask >= 0 && (mask & (1 << index)) != 0 ? LitColor : DarkColor);
                _lamps[index].SetPropertyBlock(_properties);
            }
            RefreshPrompt();
        }

        private void EndInput()
        {
            _editing = false;
            RefreshPrompt();
        }

        private void RefreshPrompt()
        {
            if (!_puzzle.IsReady) _displayText.text = "Puzzle chưa sẵn sàng.\nKiểm tra cấu hình / kết nối mạng.";
            else if (_puzzle.IsSolved) _displayText.text = "ĐÃ TẮT HẾT ĐÈN\nLỐI ĐI ĐÃ MỞ";
            else _displayText.text = _editing
                ? "1–9: chọn ô | R: đặt lại | E: thoát\nTắt hết chín đèn để mở cổng."
                : "Tắt hết đèn để mở cổng.\nĐứng gần và nhấn E để chơi.";
        }
    }
}
