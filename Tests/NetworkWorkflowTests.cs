using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

internal static class NetworkWorkflowTests
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private sealed class Rooms : IRelayRoomService
    {
        public int Signs, Creates, Joins;
        public string Code;
        public TaskCompletionSource<bool> Pending;
        public bool Throw;
        public async Task SignInAsync(CancellationToken token)
        {
            Signs++;
            if (Pending != null) await Pending.Task;
            if (Throw) throw new Exception("offline");
        }
        public Task<RelayRoom> CreateAsync(CancellationToken token)
        { Creates++; return Task.FromResult(new RelayRoom("ABC123", default)); }
        public Task<RelayRoom> JoinAsync(string code, CancellationToken token)
        { Joins++; Code = code; return Task.FromResult(new RelayRoom(code, default)); }
    }

    public static void Main()
    {
        CheckRounds();
        CheckRespawns();
        CheckSessions().GetAwaiter().GetResult();
        Console.WriteLine("PASS: " + checks + " workflow assertions (engine/network doubles).");
    }

    private static void CheckRespawns()
    {
        var prep = new NestRoundPreparation(new Transform(),
            new Transform { position = Vector3.left * 2 }, new Transform { position = Vector3.right * 2 }, null);
        var host = new NetworkObject();
        var guest = new NetworkObject();
        Vector3 initial = new Vector3(45, 2.1f, 30);
        Vector3 checkpoint = new Vector3(41.4f, 3.7f, 156.6f);
        int releases = 0;
        prep.Released += () => releases++;
        Time.unscaledTime = 0;
        prep.Configure(initial, .2f, 15f);
        prep.Begin(host, guest);
        Check(((host.transform.position + guest.transform.position) * .5f - initial).sqrMagnitude < .001f,
            "Initial preparation uses configured Start Center");
        prep.Step(host, guest);
        Check(host.Movement.Sequence == 1 && guest.Movement.Sequence == 1,
            "Explicit Begin must not issue a second teleport on next Step");
        for (int i = 0; i < 8; i++) prep.Step(host, guest);
        Check(releases == 1, "Initial round releases one fresh egg");

        prep.Configure(checkpoint, .2f, 15f);
        prep.Begin(host, guest);
        Check(prep.ReleasedSequence == 0 && host.Movement.Sequence == 2 && guest.Movement.Sequence == 2,
            "Death/egg break locks movement and gives both owners a new preparation sequence");
        Check(((host.transform.position + guest.transform.position) * .5f - checkpoint).sqrMagnitude < .001f,
            "Both players are placed around the checkpoint instead of initial spawn");
        Check(Math.Abs(Vector3.Distance(host.transform.position, guest.transform.position) - 6f) < .001f,
            "Respawn preserves the carrying distance between players");
        for (int i = 0; i < 8; i++) prep.Step(host, guest);
        Check(releases == 2 && prep.ReleasedSequence == 2, "Checkpoint respawn releases exactly one new egg");
        host.transform.position = Vector3.zero;
        guest.transform.position = Vector3.zero;
        prep.Begin(host, guest);
        for (int i = 0; i < 8; i++) prep.Step(host, guest);
        Check(releases == 3 && ((host.transform.position + guest.transform.position) * .5f - checkpoint).sqrMagnitude < .001f,
            "Repeated failure retains the checkpoint");
        prep.Reset();
        guest = new NetworkObject();
        prep.Begin(host, guest);
        uint sequence = host.Movement.Sequence;
        prep.Step(host, guest);
        Check(host.Movement.Sequence == sequence && guest.Movement.Sequence == sequence,
            "Rejoining player does not cause duplicate Begin after explicit respawn");
    }

    private static void CheckRounds()
    {
        var prep = new NestRoundPreparation(new Transform(),
            new Transform { position = Vector3.left * 2 }, new Transform { position = Vector3.right * 2 }, null);
        prep.Configure(Vector3.zero, .2f, 15f);
        var host = new NetworkObject(); var guest = new NetworkObject();
        int started = 0, placed = 0, released = 0;
        prep.Started += () => started++;
        prep.Place += (a, b) => placed++;
        prep.Released += () => released++;
        Time.unscaledTime = 0;
        Check(!prep.Step(host, guest) && started == 1, "Pair starts preparation");
        Check(host.Movement.Sequence == 1 && guest.Movement.Sequence == 1, "Both owners get same sequence");
        prep.Step(host, guest); prep.Step(host, guest); prep.Step(host, guest);
        Check(placed == 1 && released == 0 && prep.Stage == NestRoundStage.PlacingNest, "Place before release");
        Check(!prep.Step(host, guest) && released == 1 && prep.ReleasedSequence == 1, "Release on next physics step");
        Check(prep.Step(host, guest) && released == 1, "Playing does not reset egg repeatedly");
        prep.Reset();
        Check(prep.ReleasedSequence == 0 && prep.Stage == NestRoundStage.WaitingForPlayers, "Disconnect locks round");
        guest.Movement.Clear = false;
        prep.Step(host, guest);
        Check(prep.Stage == NestRoundStage.SetupFailed && released == 1, "Obstacle fails without releasing");
        guest.Movement.Clear = true;
        prep.Begin(host, guest);
        Check(host.Movement.Sequence == 3 && prep.ReleasedSequence == 0, "Retry uses fresh sequence");
        guest.Movement.Accepted = false;
        Time.unscaledTime = 16;
        prep.Step(host, guest);
        Check(prep.Stage == NestRoundStage.SetupFailed, "Missing owner acknowledgment times out");
        guest.Movement.Accepted = true;
        Check(!prep.Step(host, guest), "Late reply cannot revive failed round");
        prep.Begin(host, guest);
        guest.Movement.Rejected = true;
        prep.Step(host, guest);
        Check(prep.Stage == NestRoundStage.SetupFailed, "Owner rejection fails immediately");
        guest.Movement.Rejected = false;
        prep.Begin(host, guest);
        prep.Step(host, guest); prep.Step(host, guest); prep.Step(host, guest);
        guest.Movement.transform.position += Vector3.right;
        prep.Step(host, guest);
        Check(prep.Stage == NestRoundStage.PreparingPlayers && released == 1, "Motion after placement cancels release");
    }

    private static async Task CheckSessions()
    {
        var manager = new NetworkManager(); var rooms = new Rooms(); var transport = new UnityTransport();
        using (var session = new RelaySession(manager, transport, rooms, CancellationToken.None))
        {
            await session.ConnectAsync(false, "   ");
            Check(rooms.Signs == 0 && !session.Busy, "Empty code rejected before services");
            await session.ConnectAsync(true, "");
            Check(manager.IsHost && rooms.Creates == 1 && session.RoomCode == "ABC123", "Create and start host");
            await session.ConnectAsync(true, "");
            Check(rooms.Creates == 1, "Already listening cannot create twice");
            session.Disconnect();
            Check(session.RoomCode == "" && session.Status == "Da roi phong.", "Intentional disconnect clears room");
        }
        manager = new NetworkManager(); rooms = new Rooms();
        using (var session = new RelaySession(manager, transport, rooms, CancellationToken.None))
        {
            Time.realtimeSinceStartup = 0;
            await session.ConnectAsync(false, " abc123 ");
            Check(rooms.Code == "ABC123" && manager.Starts == 1, "Join normalizes code");
            Time.realtimeSinceStartup = 21; session.Tick();
            Check(manager.Shutdowns == 1, "Client connection times out");
            await session.ConnectAsync(false, "abc123"); manager.Connected();
            Check(session.Status == "Da ket noi voi Host!", "Connected callback updates status");
            Time.realtimeSinceStartup = 100; session.Tick();
            Check(manager.Shutdowns == 1, "Established connection does not time out");
            manager.Fail();
            Check(session.Status.StartsWith("Loi duong truyen"), "Transport failure shown");
            string status = session.Status; session.Dispose(); manager.Connected();
            Check(session.Status == status, "Dispose removes callbacks");
        }
        manager = new NetworkManager(); rooms = new Rooms { Pending = new TaskCompletionSource<bool>() };
        var pendingSession = new RelaySession(manager, transport, rooms, CancellationToken.None);
        Task pending = pendingSession.ConnectAsync(true, "");
        await pendingSession.ConnectAsync(true, "");
        Check(rooms.Signs == 1 && pendingSession.Busy, "Concurrent requests ignored");
        pendingSession.Dispose(); rooms.Pending.SetResult(true); await pending;
        Check(manager.Starts == 0 && rooms.Creates == 0, "Destroy during auth never starts networking");
        manager = new NetworkManager(); rooms = new Rooms { Throw = true };
        using (var session = new RelaySession(manager, transport, rooms, CancellationToken.None))
        {
            await session.ConnectAsync(true, "");
            Check(!session.Busy && session.Status.Contains("offline"), "Failure permits retry");
            rooms.Throw = false; manager.StartSucceeds = false;
            await session.ConnectAsync(true, "");
            Check(!session.Busy && session.RoomCode == "" && session.Status.StartsWith("Loi:"), "Start failure does not expose room code");
        }
    }
}
