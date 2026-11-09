using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("_puzzle")]
        [SerializeField] private LinkedLightsPuzzle puzzle;

        [FormerlySerializedAs("_displayText")]
        [SerializeField] private TMP_Text displayText;

        [Tooltip("Đúng chín renderer, thứ tự trái sang phải, trên xuống dưới.")]
        [FormerlySerializedAs("_lamps")]
        [SerializeField] private MeshRenderer[] lamps;

        [Tooltip("Màn hình UI phóng to (kích thước bằng Setting Panel) khi nhấn E tương tác.")]
        [SerializeField] private LinkedLightsZoomUI zoomUI;

        private readonly HashSet<Collider> localPlayers = new HashSet<Collider>();
        private MaterialPropertyBlock properties;
        private bool isEditing;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            bool valid = puzzle != null && displayText != null && lamps != null && lamps.Length == 9;
            if (valid)
                foreach (MeshRenderer lamp in lamps) valid &= lamp != null;
            if (!valid)
            {
                Debug.LogError("[LinkedLightsConsole] Cần puzzle, bảng trạng thái và đúng chín đèn theo thứ tự 1–9.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (puzzle == null || properties == null || displayText == null || lamps == null || lamps.Length != 9) return;
            puzzle.StateChanged += HandleStateChanged;
            HandleStateChanged(puzzle.IsReady ? puzzle.Mask : -1);
        }

        private void OnDisable()
        {
            if (puzzle != null) puzzle.StateChanged -= HandleStateChanged;
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
                if (isEditing) EndInput();
                return;
            }

            Keyboard keys = Keyboard.current;
            if (keys == null) return;

            // Nhấn E để bật / tắt tương tác và mở màn hình phóng to
            if (keys.eKey.wasPressedThisFrame)
            {
                if (!isEditing)
                {
                    StartInput();
                }
                else
                {
                    EndInput();
                }
                return;
            }

            if (!isEditing) return;

            // Nhấn R để đặt lại câu đố
            if (keys.rKey.wasPressedThisFrame)
            {
                puzzle.RequestReset();
                return;
            }

            // Nhấn phím số 1–9 để đảo ô
            for (int index = 0; index < 9; index++)
            {
                if (keys[DigitKeys[index]].wasPressedThisFrame || keys[NumpadKeys[index]].wasPressedThisFrame)
                    puzzle.RequestPress(index + 1);
            }
        }

        private void StartInput()
        {
            isEditing = true;
            EnsureZoomUI();
            if (zoomUI != null)
            {
                zoomUI.Open(puzzle, () =>
                {
                    isEditing = false;
                    RefreshPrompt();
                });
            }
            RefreshPrompt();
        }

        private void EnsureZoomUI()
        {
            if (zoomUI == null)
            {
                zoomUI = FindFirstObjectByType<LinkedLightsZoomUI>(FindObjectsInactive.Include);
            }
        }

        private void HandleStateChanged(int mask)
        {
            if (mask < 0 || mask == 0)
            {
                if (mask == 0 && isEditing)
                {
                    EndInput();
                }
            }

            for (int index = 0; index < 9; index++)
            {
                properties.Clear();
                properties.SetColor(BaseColor, mask >= 0 && (mask & (1 << index)) != 0 ? LitColor : DarkColor);
                lamps[index].SetPropertyBlock(properties);
            }
            RefreshPrompt();
        }

        private void EndInput()
        {
            isEditing = false;
            if (zoomUI != null && zoomUI.IsOpen)
            {
                zoomUI.Close();
            }
            RefreshPrompt();
        }

        private void RefreshPrompt()
        {
            if (!puzzle.IsReady) displayText.text = "Puzzle chưa sẵn sàng.\nKiểm tra cấu hình / kết nối mạng.";
            else if (puzzle.IsSolved) displayText.text = "ĐÃ TẮT HẾT ĐÈN\nLỐI ĐI ĐÃ MỞ";
            else displayText.text = isEditing
                ? "1–9: chọn ô | R: đặt lại | E: thoát\nTắt hết chín đèn để mở cổng."
                : "Tắt hết đèn để mở cổng.\nĐứng gần và nhấn E để chơi.";
        }
    }
}
