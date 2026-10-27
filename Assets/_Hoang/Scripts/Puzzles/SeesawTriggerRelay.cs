using UnityEngine;

namespace Hoang.Puzzles
{
    // Chuyển tiếp sự kiện va chạm trigger từ khu vực mặt cầu tới SeesawBridge.
    public class SeesawTriggerRelay : MonoBehaviour
    {
        [SerializeField] private SeesawBridge _bridge;

        public void Initialize(SeesawBridge bridge) => _bridge = bridge;

        private void OnTriggerEnter(Collider other)
        {
            if (_bridge != null) _bridge.RegisterPlayer(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_bridge != null) _bridge.UnregisterPlayer(other);
        }
    }
}
