using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

// Test view: draws controls and forwards commands to the session or nest.
[RequireComponent(typeof(NetworkManager))]
public class NetworkTestUI : MonoBehaviour
{
    private NetworkManager manager;
    private RelaySession session;
    private string joinCodeInput = "";
    private NetworkNest testNest;

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();
        session = new RelaySession(manager, GetComponent<UnityTransport>(),
            new RelayRoomService(), destroyCancellationToken);
    }

    private void Update() => session.Tick();
    private void OnDestroy() => session?.Dispose();
    private void OnGUI()
    {
        if (manager == null) return;

        GUILayout.BeginArea(
            new Rect(20, 20, 360, 490), GUI.skin.box);

        GUILayout.Label("ONLINE CO-OP - RELAY");
        GUILayout.Space(8);

        bool active = manager.IsClient || manager.IsServer;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled &&
                      !session.Busy &&
                      !manager.ShutdownInProgress;

        if (!active)
        {
            if (GUILayout.Button("Create Room", GUILayout.Height(40)))
                _ = session.ConnectAsync(true, joinCodeInput);

            GUILayout.Space(10);
            GUILayout.Label("Nhap ma phong:");
            joinCodeInput = GUILayout.TextField(joinCodeInput, 12);

            if (GUILayout.Button("Join Room", GUILayout.Height(40)))
                _ = session.ConnectAsync(false, joinCodeInput);
        }
        else
        {
            if (testNest == null)
                testNest = FindFirstObjectByType<NetworkNest>();
            if (manager.IsHost)
            {
                GUILayout.Label("Ban la Host");
                GUILayout.Label("Ma phong: " + session.RoomCode);
                GUILayout.Label(
                    "So nguoi: " + manager.ConnectedClientsIds.Count + "/2");

                if (GUILayout.Button("Copy Code"))
                    GUIUtility.systemCopyBuffer = session.RoomCode;
            }
            else
            {
                GUILayout.Label(manager.IsConnectedClient
                    ? "Client - Da ket noi"
                    : "Client - Dang ket noi...");
            }

            if (testNest != null && testNest.IsSpawned && testNest.HasEggTest)
            {
                GUILayout.Space(8);
                switch (testNest.RoundStage)
                {
                    case NestRoundStage.WaitingForPlayers:
                        GUILayout.Label("Dang cho du 2 nguoi...");
                        break;
                    case NestRoundStage.PreparingPlayers:
                        GUILayout.Label("Dang chuan bi: dat vi tri va cho xac nhan...");
                        break;
                    case NestRoundStage.PlacingNest:
                        GUILayout.Label("Dang dat to va trung...");
                        break;
                    case NestRoundStage.SetupFailed:
                        GUILayout.Label("Chuan bi that bai. Host xem Console va thu lai.");
                        if (manager.IsHost && GUILayout.Button("Retry Setup"))
                            testNest.RetryPreparationOnServer();
                        break;
                }
                GUILayout.Label(testNest.EggPhase == NestEggPhase.Waiting
                    ? "Trung: Chua tha"
                    : testNest.EggPhase == NestEggPhase.Intact
                        ? "Trung: Nguyen ven"
                        : "Trung: Da vo (mau do) - Host bam Reset");
                if (manager.IsHost && testNest.CanResetEgg && testNest.RoundStage == NestRoundStage.Playing)
                {
                    if (GUILayout.Button("Reset Egg (test)", GUILayout.Height(30)))
                        testNest.ResetEggOnServer();
                    if (GUILayout.Button("Drop Egg (test)", GUILayout.Height(30)))
                        testNest.DropEggForTestOnServer();
                }
            }

            if (GUILayout.Button("Disconnect", GUILayout.Height(40)))
            {
                session.Disconnect();
            }
        }

        GUI.enabled = previousEnabled;

        GUILayout.Space(10);
        GUILayout.Label(session.Status);
        GUILayout.EndArea();
    }
}
