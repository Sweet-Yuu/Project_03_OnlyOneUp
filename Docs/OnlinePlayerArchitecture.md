# Online player: responsibilities after refactoring

`NetworkTestMovement` is the network adapter and composition root. Its MonoScript
GUID, serialized field names, RPC names/permissions, NetworkVariable and public
methods consumed by `NetworkNest` remain in place. Existing prefab values do not
need migration. Helper classes are plain C# objects, not additional components.

| Class | Responsibility |
| --- | --- |
| `NetworkTestMovement` | Owner/server guards, RPC transport, replicated animation state, lifecycle and composition |
| `KeyboardPlayerInput` / `IOnlinePlayerInput` | Read a local input frame; can be replaced with another input device |
| `OnlinePlayerStateMachine` + Idle/Walk states | Decide movement state from supplied input, without reading devices or networking |
| `PlayerCharacterMotor` | CharacterController motion, gravity, jumping and vertical collision response |
| `IPlayerMovementConstraint` | Narrow extension point for constraining requested displacement |
| `CarryDistanceConstraint` | Pure minimum/maximum 3D separation calculation and bounded correction |
| `PlayerSpawnProbe` | Starting floor raycasts and clearance queries |
| `PlayerPreparation` | Sequence/acknowledgment state and readiness checks, independent of RPC transport |
| `LocalPlayerCamera` | Bind/restore the local camera and update the fallback test camera |
| `PlayerAnimationPresenter` | Validate the Animator and play the supplied Idle/Walk state |

## Runtime flow

1. The network adapter checks spawn, ownership and preparation readiness.
2. The local input source supplies a frame to the state machine.
3. The state machine supplies movement intent to the motor. Idle still runs
   gravity and can jump; only the preparation gate freezes the motor.
4. The motor applies the movement constraint before CharacterController.Move.
5. The adapter keeps the player facing their partner and publishes animation
   state. Remote players only present replicated animation and NetworkTransform.
6. On despawn, subscriptions are removed and the local camera is restored.

RPC validation stays in the adapter; physics placement stays on the owner as
before. Unity-specific helpers still depend on Unity APIs. Interfaces are used
at the replaceable input and movement-constraint boundaries, rather than adding
an interface to every class.

## Intentionally preserved behavior

- Waiting spawn positions are still the prototype coordinates before setup.
- Root distance still assumes forward anchors on flat starting ground.
- The user's extra **10 units** of maximum carry reach is preserved as
  `ExtraCarryReach` in `NetworkTestMovement`.
- Input remains world-axis WASD/arrows, Space to jump, only while focused.
- Walk animation follows input intent, including when blocked by an obstacle.
- Cinemachine selection and fallback behavior are unchanged.
- Offline PlayerMovement/IdleState/WalkState are not migrated in this change.

## Validation

Run `Tests/RunPlayerLogicTests.ps1`, optionally supplying `-UnityEditorPath`.
These standalone tests use the production managed logic and Unity Vector types.
They cover carry bounds, overlapping players, bounded correction, 3D distance,
stale setup replies, rejection/retry/session reset, and Idle/Walk/jump input.
They do not exercise Unity physics, NGO code generation or transport.

Manual Editor/build checks still required:

1. Host alone: waiting input locked; local Cinemachine target follows local player.
2. Join client: both are placed, preparation completes and egg is released.
3. Both owners: walk, stop, jump, collide with a ceiling and reach carry limits.
4. Observe both players' animation on both peers; remote input must not move them.
5. Failed placement/retry: old acknowledgments must not release the new round.
6. Disconnect/rejoin: camera target restores, setup resets, no duplicate callbacks.

When VS Code has unsaved buffers, preserve those edits before reloading the
changed files from disk. Do not overwrite this refactor with an old buffer.
