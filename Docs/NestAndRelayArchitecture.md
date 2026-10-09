# Nest and Relay responsibilities

The existing `NetworkNest` and `NetworkTestUI` components stay attached to their
scene objects. Their script GUIDs are unchanged. All NetworkNest Inspector field
names/defaults, public entry points and NetworkVariable declarations/order are
preserved. The extracted helpers are plain C# objects; do not add them as components.

## Nest

| Class | Responsibility |
| --- | --- |
| NetworkNest | Network lifecycle, server authority, replicated round state and paired nest/egg snapshots |
| NestCarryPose | Validate grip geometry and calculate the nest pose |
| NestRoundPreparation | Validate both starting players and coordinate readiness, stability, placement and release |
| RoundPreparationGate | Pure stability/timeout state machine, unchanged |
| NestEggLifecycle | Create/destroy/reset/drop the egg and maintain player collision exclusions |
| NestEggFrameBuffer | Buffer/interpolate paired snapshots, unchanged |

The preparation workflow emits Started, Place and Released commands. NetworkNest
handles those commands and copies the workflow's Stage/ReleasedSequence into the
existing NetworkVariables. Only the server calls the workflow. A Place event does
not release the egg: readiness and stability must survive the next physics step.

The model/physics details inside NestEggBody are outside this refactor. Its current
collision rules are unchanged. Snapshot timestamp, generation, buffering delay,
legacy movement-only scenes and the existing grip/carry-height formulas are kept.

## Relay

| Class | Responsibility |
| --- | --- |
| NetworkTestUI | Draw the test HUD and forward user commands |
| RelaySession | Connection lifecycle/status, NetworkManager callbacks, timeout, disconnect and cancellation |
| IRelayRoomService | Authentication/allocation operations required by the session |
| RelayRoomService | Unity Services authentication and Relay create/join implementation |

The room service returns a room code and RelayServerData. It does not start a
NetworkManager or update GUI text. The session configures the transport, starts
host/client and exposes Busy/Status/RoomCode to the view. All asynchronous entry
points return Task. Destroying the view disposes the session, cancels pending
continuations and unsubscribes callbacks; successful allocation after cancellation
must not start networking. Underlying SDK requests may still complete remotely.

This is separation of responsibilities, not elimination of Unity dependencies.
Network adapters still know NGO and Unity lifecycle; geometry/physics helpers
still know Unity types. The Relay backend can be replaced through its interface.

## Verification

Run `Tests/RunNetworkWorkflowTests.ps1` (optional `-UnityEditorPath`). It compiles
production NestRoundPreparation, RoundPreparationGate and RelaySession against
boundary doubles in Tests, using real managed Unity Vector types. It covers:

- Placement before release, release exactly once, disconnect/reset.
- Obstructed start, retry sequence, acknowledgment timeout, late reply, owner
  rejection and movement during placement.
- Empty/normalized join codes, duplicate requests, host/join, timeout, disconnect,
  callback cleanup, authentication cancellation, failures and retry availability.

The doubles are outside Assets and are not included in the game. These tests do
not verify native Rigidbody/collider behavior, NGO serialization/code generation,
Cinemachine or actual Unity Services/Relay connectivity.

Manual checks in Unity using matching builds remain necessary:

1. Create and join a room; confirm 2/2 players, stable setup and egg release.
2. Walk/jump on both peers; nest and egg remain on the same presentation timeline.
3. Reset Egg, Drop Egg, Retry Setup after a blocked spawn.
4. Disconnect/rejoin, incorrect code, and stop Play Mode while creating/joining.
5. Movement-only test scene still operates with its existing NetworkTransform.

Keep a copy of any unsaved VS Code edits before reloading these files from disk.
