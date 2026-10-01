using Unity.Netcode;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    private void OnGUI()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
            return;

        GUILayout.BeginArea(new Rect(20, 20, 300, 230));

        if (!manager.IsClient && !manager.IsServer)
        {
            if (GUILayout.Button("Host", GUILayout.Height(40)))
                manager.StartHost();

            if (GUILayout.Button("Client", GUILayout.Height(40)))
                manager.StartClient();
        }
        else
        {
            string role = manager.IsHost ? "Host" :
                manager.IsConnectedClient ? "Client - Da ket noi" : "Client - Dang ket noi...";
            GUILayout.Label("Dang chay: " + role);
            if (manager.IsServer)
                GUILayout.Label("So nguoi: " + manager.ConnectedClientsIds.Count);
            GUILayout.Label("WASD / mui ten: Di chuyen | Space: Nhay");

            if (GUILayout.Button("Disconnect", GUILayout.Height(40)))
                manager.Shutdown();
        }

        GUILayout.EndArea();
    }
}
