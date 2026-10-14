using Unity.Netcode;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
[DefaultExecutionOrder(500)]
public class NetworkNest : NetworkBehaviour, IEggJumpReceiver, IRoundResetTarget
{
    [SerializeField] private Transform leftGrip;
    [SerializeField] private Transform rightGrip;
    [SerializeField] private float carryHeight = 20f;
    [Header("Host-authoritative egg test")]
    [SerializeField] private bool enableEggTest;
    [SerializeField] private GameObject eggVisualPrefab;
    [SerializeField] private bool _progressiveEggDamage;
    [SerializeField] private GameObject _brokenEggVisualPrefab;
    [Header("Local egg damage feedback")]
    [SerializeField, Min(0.01f)] private float _eggDamageShakeDuration = .35f;
    [SerializeField, Min(0f)] private float _eggDamageShakeStrength = .07f;
    [SerializeField, Min(0.01f)] private float _eggDamageFlashDuration = .9f;
    private EggDamageFeedback _damageFeedback;
    [Header("Nest collision consequences")]
    [SerializeField] private bool _randomEggOnNestImpact = true;
    [SerializeField, Range(0f, 1f)] private float _nestImpactCrackChance = 0.5f;
    [SerializeField, Min(0f)] private float _nestImpactCooldown = 0.75f;
    [SerializeField] private LayerMask _nestImpactLayers = ~0;
    [SerializeField, Min(0f)] private float _impactFallUpSpeed = 1f;
    [SerializeField, Min(0f)] private float _impactFallSideSpeed = 2f;
    private NestImpactDetector _impactDetector;
    private float _nextNestImpact;
    private uint _impactSequence;
    [SerializeField] private bool _showEggTrajectory;
    private EggFallTrajectory _fallTrajectory;
    [SerializeField] private BoxCollider nestFloor;
    [SerializeField, Min(0f)] private float eggDropClearance = 0.2f;
    [SerializeField, Range(0.05f, 0.25f)] private float presentationDelay = 0.1f;
    [Header("Cooperative egg physics (scene opt-in)")]
    [SerializeField] private bool _assistEggCarry;
    [Tooltip("Maximum time between the two actual takeoffs, in seconds.")]
    [SerializeField, Range(0.05f, 1f)] private float _combinedJumpWindow = 0.2f;
    [Tooltip("Distance from the resting point to the rim, measured at the egg centre. The current nest fits 0.75 units.")]
    [SerializeField, Min(0f)] private float _eggTravel = 0.75f;
    [Tooltip("Release the egg to gravity when the nest tilts beyond this angle from world up.")]
    [SerializeField, Range(0f, 90f)] private float _eggReleaseTilt = 70f;
    [Tooltip("Upward launch speed added to the nest's point velocity on a paired jump.")]
    [SerializeField, Min(0f)] private float _eggLaunchUp = 15f;
    [SerializeField, Min(0f)] private float _eggLaunchSide = 5f;
    private INestEggCarryPhysics _eggCarryPhysics;
    private double _eggRoundStart;
    [Header("Smooth online carry (scene opt-in)")]
    [SerializeField] private bool _smoothOnlineCarry;
    [SerializeField] private bool _predictCarryInputs;
    [SerializeField, Range(0.01f, 0.1f)] private float _carryInputResponseTime = 0.025f;
    private INestCarrySimulation _carrySimulation;
    private NestCarryPrediction _carryPrediction;
    private readonly NestCarryInputBuffer _carryInputs = new NestCarryInputBuffer();
    private readonly NestReconciledPresentation _reconciledPresentation = new NestReconciledPresentation();
    private NestCarryState _serverCarryState;
    private Vector3 _lastGuestCarryAnchor;
    [SerializeField, Range(0.02f, 0.3f)] private float _serverSmoothTime = 0.1f;
    [SerializeField, Range(0.01f, 0.2f)] private float _clientSmoothTime = 0.04f;
    [SerializeField, Range(0.05f, 0.5f)] private float _clientCorrectionTime = 0.2f;
    [SerializeField, Min(0.1f)] private float _clientCorrectionSpeed = 4f;
    [SerializeField, Min(1f)] private float _clientCorrectionAngularSpeed = 90f;
    [SerializeField, Min(0f)] private float _maxPresentationOffset = 3f;
    [SerializeField, Range(0f, 45f)] private float _maxPresentationAngle = 20f;
    private readonly NestPositionSmoother _serverPosition = new NestPositionSmoother();
    private readonly NestClientPresentation _clientPresentation = new NestClientPresentation();
    private NetworkTestMovement _hostMovement;
    private NetworkTestMovement _guestMovement;
    private Coroutine _cachePlayersRoutine;
    [Header("Round preparation (flat starting ground)")]
    [Tooltip("Fallback for scenes without IRoundSession. MainMap_Online uses Start Center on NetworkRoundManager.")]
    [SerializeField] private Vector3 startCenter = new Vector3(0f, 2.1f, 0f);
    private IRoundSession _roundSession;
    private bool IsRoundSuspended => _roundSession != null && _roundSession.IsRespawning;
    private Vector3 PreparationCenter => _roundSession != null ? _roundSession.RespawnCenter : startCenter;
    public event System.Action RoundFailed;
    public event System.Action<bool> WinChanged;
    private readonly NetworkVariable<bool> _hasWon = new NetworkVariable<bool>();
    private NestWinContact _winContact;
    public bool HasWon => IsSpawned && _hasWon.Value;
    private void OnWinChanged(bool previous, bool current) => WinChanged?.Invoke(current);
    public bool IsPlaying => IsSpawned && validSetup && enableEggTest && roundStage.Value == NestRoundStage.Playing;
    [SerializeField, Min(0.1f)] private float stableDuration = 0.2f;
    [SerializeField, Min(2f)] private float preparationTimeout = 15f;
    private readonly NetworkVariable<NestRoundStage> roundStage = new NetworkVariable<NestRoundStage>();
    private readonly NetworkVariable<uint> releasedPreparation = new NetworkVariable<uint>();
    private NestRoundPreparation preparation;
    private readonly NestCarryPose carryPose = new NestCarryPose();
    private NestEggLifecycle eggs;
    private IGameEntryGate _entryGate;

    private readonly NetworkVariable<NestEggFrame> frame = new NetworkVariable<NestEggFrame>();
    private readonly NestEggFrameBuffer history = new NestEggFrameBuffer();
    private Rigidbody body;
    private NestEggBody egg => eggs?.Egg;
    private bool validSetup;
    private bool placed;
    private uint generation;
    private double nextSnapshot;
    private NestEggPhase phase;

    public float GripDistance => leftGrip == null || rightGrip == null ? 0f :
        Vector3.Distance(leftGrip.position, rightGrip.position);
    public NestEggPhase EggPhase => phase;
    public bool HasEggTest => enableEggTest;
    public bool CarryReady => IsSpawned && validSetup &&
        (enableEggTest ? roundStage.Value == NestRoundStage.Playing : true);
    public Transform GetGrip(ulong owner) =>
        owner == Unity.Netcode.NetworkManager.ServerClientId ? leftGrip : rightGrip;

    public Vector3 PredictGrip(ulong owner, Vector3 hostAnchor, Vector3 guestAnchor)
    {
        if (!NestCarryPose.IsValidAxis(guestAnchor - hostAnchor)) return GetGrip(owner).position;
        Pose pose = carryPose.Calculate(hostAnchor, guestAnchor);
        return pose.position + pose.rotation * transform.InverseTransformPoint(GetGrip(owner).position);
    }
    public NestRoundStage RoundStage => roundStage.Value;
    public bool CanPlayerMove(uint sequence) => IsSpawned && (_entryGate == null || _entryGate.IsOpen) &&
    !IsRoundSuspended &&
    roundStage.Value == NestRoundStage.Playing &&
    sequence != 0 &&
    releasedPreparation.Value == sequence;
    public bool CanResetEgg => IsSpawned && IsServer && placed && egg != null && !IsRoundSuspended;

    private void Awake()
    {
        _roundSession = GetComponent<IRoundSession>();
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        preparation = new NestRoundPreparation(transform, leftGrip, rightGrip, this);
        preparation.Started += OnPreparationStarted;
        preparation.Place += PlacePreparedNest;
        preparation.Released += ResetEggOnServer;
        eggs = new NestEggLifecycle(transform, body, nestFloor);
        eggs.Broken += OnEggBroken;
        _entryGate = GetComponent<IGameEntryGate>();
        _impactDetector = new NestImpactDetector(transform);
    }

    public override void OnNetworkSpawn()
    {
        _hasWon.OnValueChanged += OnWinChanged;
        if (IsServer)
        {
            _hasWon.Value = false;
            _winContact = new NestWinContact(transform);
        }
        WinChanged?.Invoke(_hasWon.Value);
        validSetup = false;
        placed = false;
        phase = NestEggPhase.Waiting;
        history.Clear();
        _serverPosition.Reset(body.position);
        preparation.Reset();
        if (IsServer)
        {
            roundStage.Value = NestRoundStage.WaitingForPlayers;
            releasedPreparation.Value = 0;
        }
        body.interpolation = IsServer ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;

        if (leftGrip == null || rightGrip == null || (enableEggTest && nestFloor == null))
        {
            Debug.LogError("Nest: assign LeftGrip, RightGrip and the Bottom BoxCollider.", this);
            return;
        }
        if (!carryPose.Initialize(transform, leftGrip, rightGrip, out string error))
        {
            Debug.LogError(error, this);
            return;
        }
        _carrySimulation = new NestCarrySimulation(carryPose, _carryInputResponseTime);
        _carryPrediction = new NestCarryPrediction(_carrySimulation);
        // Existing movement-only scenes keep their NetworkTransform setup.
        if (!enableEggTest)
        {
            validSetup = true;
            return;
        }
        eggs.Create(eggVisualPrefab, IsServer);
        if (_showEggTrajectory && IsClient) _fallTrajectory = new EggFallTrajectory(transform, egg.transform);
        if (_progressiveEggDamage) egg.ConfigureProgressiveDamage(_brokenEggVisualPrefab);
        if (_progressiveEggDamage && IsClient)
        {
            // Canvas overlay ở gốc scene để không nhận chuyển động/quay của tổ.
            var feedbackObject = new GameObject("Local egg damage feedback", typeof(RectTransform));
            _damageFeedback = feedbackObject.AddComponent<EggDamageFeedback>();
            _damageFeedback.Initialize(Camera.main, _eggDamageShakeDuration,
                _eggDamageShakeStrength, _eggDamageFlashDuration);
        }
        if (IsServer && _assistEggCarry)
        {
            _eggCarryPhysics = new NestEggCarryPhysics(egg, _combinedJumpWindow,
                _eggTravel, _eggLaunchUp, _eggLaunchSide, _eggReleaseTilt);
            eggs.SetCarryPhysics(_eggCarryPhysics);
        }
        validSetup = true;

        if (IsServer)
        {
            generation = frame.Value.Generation + 1;
            PublishFrame();
        }
        else
        {
            frame.OnValueChanged += ReceiveFrame;
            ReceiveFrame(default, frame.Value);
            if (_smoothOnlineCarry || _predictCarryInputs)
                _cachePlayersRoutine = StartCoroutine(CachePresentationPlayers());
        }
    }

    private IEnumerator CachePresentationPlayers()
    {
        var interval = new WaitForSecondsRealtime(0.25f);
        while (IsSpawned)
        {
            if (_hostMovement == null || !_hostMovement.IsSpawned ||
                _guestMovement == null || !_guestMovement.IsSpawned)
            {
                _hostMovement = _guestMovement = null;
                // Client không có ConnectedClientsList. Cache sau khi player được spawn.
                foreach (NetworkObject player in NetworkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (!player.IsPlayerObject || !player.TryGetComponent(out NetworkTestMovement movement)) continue;
                    if (player.OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId) _hostMovement = movement;
                    else _guestMovement = movement;
                }
            }
            yield return interval;
        }
    }


    private bool TryGetPlayers(out NetworkObject host, out NetworkObject guest)
    {
        host = guest = null;
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.ClientId == Unity.Netcode.NetworkManager.ServerClientId) host = client.PlayerObject;
            else guest = client.PlayerObject;
        }
        return host != null && guest != null;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !validSetup) return;
        if (!IsServer)
        {
            PredictCarryInput();
            return;
        }
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest))
        {
            if (placed || (enableEggTest && roundStage.Value != NestRoundStage.WaitingForPlayers))
            {
                if (enableEggTest)
                {
                    roundStage.Value = NestRoundStage.WaitingForPlayers;
                    releasedPreparation.Value = 0;
                    preparation.Reset();
                }
                placed = false;
                phase = NestEggPhase.Waiting;
                eggs.Wait();
                generation++;
                PublishFrame();
            }
            return;
        }

        if (IsRoundSuspended) return;
        if (enableEggTest && !PrepareRound(host, guest)) return;

        bool duckPair = host.GetComponent<NetworkTestMovement>() is NetworkTestMovement hm && hm.HasDuckRig &&
            guest.GetComponent<NetworkTestMovement>() is NetworkTestMovement gm && gm.HasDuckRig;
        Vector3 leftTarget = enableEggTest || duckPair
            ? host.GetComponent<NetworkTestMovement>().CarryPoint
            : host.transform.position + Vector3.up * carryHeight;
        Vector3 rightTarget = enableEggTest || duckPair
            ? guest.GetComponent<NetworkTestMovement>().CarryPoint
            : guest.transform.position + Vector3.up * carryHeight;
        if (_predictCarryInputs && enableEggTest && placed)
        {
            if (_carryInputs.TryTake(out NestCarryInput input))
            {
                // Giới hạn một bước dịch chuyển bất thường; không nhận HostAnchor từ client.
                _lastGuestCarryAnchor = Vector3.MoveTowards(_lastGuestCarryAnchor, input.GuestAnchor,
                    100f * Time.fixedDeltaTime);
            }
            else if (_carryInputs.AppliedSequence == 0) _lastGuestCarryAnchor = rightTarget;
            rightTarget = _lastGuestCarryAnchor;
        }
        if (!NestCarryPose.IsValidAxis(rightTarget - leftTarget)) return;
        Pose pose = carryPose.Calculate(leftTarget, rightTarget);
        Vector3 position = pose.position;
        Quaternion rotation = pose.rotation;

        if (!placed)
        {
            body.position = position;
            body.rotation = rotation;
            _serverPosition.Reset(position);
            placed = true;
            ResetEggOnServer();
        }
        else
        {
            if (_predictCarryInputs && enableEggTest)
            {
                _serverCarryState.Position = body.position;
                _serverCarryState.Rotation = body.rotation;
                _serverCarryState = _carrySimulation.Step(_serverCarryState, new NestCarryInput
                {
                    HostAnchor = leftTarget, GuestAnchor = rightTarget
                }, Time.fixedDeltaTime);
                position = _serverCarryState.Position;
                rotation = _serverCarryState.Rotation;
            }
            else if (_smoothOnlineCarry && enableEggTest)
            {
                // Lọc mục tiêu trước MovePosition để bước nhảy của gói mạng không
                // biến thành một bước nhảy vận tốc lớn của sàn tổ trong vật lý.
                position = _serverPosition.Step(position, _serverSmoothTime, Time.fixedDeltaTime);
                rotation = Quaternion.Slerp(body.rotation, rotation,
                    1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.01f, _serverSmoothTime)));
            }
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }
        _eggCarryPhysics?.Step(new Pose(position, rotation), Time.fixedDeltaTime);
        eggs.Tick(host, guest);
        CheckNestImpact(host, guest);
    }

    private void CheckNestImpact(NetworkObject host, NetworkObject guest)
    {
        if (!_randomEggOnNestImpact || egg == null) return;
        bool entered = _impactDetector.Sample(host.transform, guest.transform,
            egg.transform, _nestImpactLayers, out Vector3 away);
        if (!entered || !IsPlaying || IsRoundSuspended || !egg.IsSimulating ||
            (_entryGate != null && !_entryGate.IsOpen) ||
            Time.unscaledTime < _nextNestImpact ||
            NetworkManager.ServerTime.Time - _eggRoundStart < 0.35 ||
            !(eggs.IsEggInNest || egg.IsSupportedByNest)) return;

        // Chỉ host bốc thăm; snapshot mang cả kết quả và số sự kiện hiệu ứng.
        _nextNestImpact = Time.unscaledTime + _nestImpactCooldown;
        _impactSequence++;
        if (Random.value < _nestImpactCrackChance)
            egg.DamageFromNestImpact();
        else
        {
            Vector3 direction = Vector3.ProjectOnPlane(away, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            _eggCarryPhysics?.Stop();
            eggs.FallFromImpact(direction, _impactFallSideSpeed, _impactFallUpSpeed);
        }
        PublishFrame();
    }

    public void ReportLocalJump()
    {
        if (!_assistEggCarry || !enableEggTest || !CarryReady) return;
        // LocalTime của NGO đã bù nửa RTT; bỏ buffer gửi sớm để ước lượng
        // thời điểm bấm thật trên đồng hồ host, không dùng lúc RPC tới nơi.
        double time = NetworkManager.LocalTime.Time - NetworkManager.NetworkTimeSystem.LocalBufferSec;
        ReportEggJumpRpc(frame.Value.Generation, time);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportEggJumpRpc(uint eggGeneration, double time, RpcParams rpcParams = default)
    {
        if (!_assistEggCarry || !enableEggTest || !CarryReady || eggGeneration != generation ||
            double.IsNaN(time) || double.IsInfinity(time) || time < _eggRoundStart ||
            time < NetworkManager.ServerTime.Time - 1.0 || time > NetworkManager.ServerTime.Time + 0.1 ||
            !TryGetPlayers(out NetworkObject host, out NetworkObject guest)) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender != host.OwnerClientId && sender != guest.OwnerClientId) return;
        _eggCarryPhysics?.ReportJump(sender == host.OwnerClientId, time);
    }

    private bool CanPredictCarry => _predictCarryInputs && enableEggTest &&
        !IsRoundSuspended &&
        _carryPrediction != null && _carryPrediction.Generation != 0 &&
        roundStage.Value == NestRoundStage.Playing && frame.Value.Phase != NestEggPhase.Waiting &&
        _hostMovement != null && _hostMovement.IsSpawned &&
        _guestMovement != null && _guestMovement.IsSpawned && _guestMovement.IsOwner;

    private void PredictCarryInput()
    {
        if (!CanPredictCarry) return;
        _carryPrediction.Predict(_hostMovement.CarryPoint, _guestMovement.CarryPoint);
        NestCarryInput[] batch = _carryPrediction.GetResendBatch();
        if (batch.Length > 0) SubmitCarryInputsRpc(batch);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    private void SubmitCarryInputsRpc(NestCarryInput[] inputs, RpcParams rpcParams = default)
    {
        if (!_predictCarryInputs || !enableEggTest || roundStage.Value != NestRoundStage.Playing ||
            inputs == null || inputs.Length > NestCarryInputBuffer.BatchSize ||
            !TryGetPlayers(out _, out NetworkObject guest) ||
            guest.OwnerClientId != rpcParams.Receive.SenderClientId) return;
        foreach (NestCarryInput input in inputs) _carryInputs.Add(input);
    }

    private bool PrepareRound(NetworkObject host, NetworkObject guest)
    {
        preparation.Configure(PreparationCenter, stableDuration, preparationTimeout);
        bool ready = preparation.Step(host, guest);
        SyncPreparation();
        return ready;
    }

    private void SyncPreparation()
    {
        roundStage.Value = preparation.Stage;
        releasedPreparation.Value = preparation.ReleasedSequence;
    }

    private void OnPreparationStarted()
    {
        placed = false;
        phase = NestEggPhase.Waiting;
        eggs.Wait();
        generation++;
        PublishFrame();
    }

    private void PlacePreparedNest(Vector3 left, Vector3 right)
    {
        Pose pose = carryPose.Calculate(left, right);
        body.position = pose.position;
        body.rotation = pose.rotation;
        _serverPosition.Reset(pose.position);
        placed = true;
        generation++;
        PublishFrame();
    }

    public void RetryPreparationOnServer()
    {
        if (!IsServer || !enableEggTest || roundStage.Value != NestRoundStage.SetupFailed) return;
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest)) return;
        preparation.Configure(PreparationCenter, stableDuration, preparationTimeout);
        preparation.Begin(host, guest);
        SyncPreparation();
    }

    public bool TryRestartRound(Vector3 center)
    {
        if (!IsSpawned || !IsServer || !validSetup || !enableEggTest ||
            !TryGetPlayers(out NetworkObject host, out NetworkObject guest)) return false;
        preparation.Configure(center, stableDuration, preparationTimeout);
        preparation.Begin(host, guest);
        SyncPreparation();
        return true;
    }

    // Capture after physics; both bodies share the same network timestamp.
    private void LateUpdate()
    {
        if (!IsSpawned || !validSetup || !enableEggTest) return;
        if (IsServer)
        {
            if (!_hasWon.Value && IsPlaying && !IsRoundSuspended && _winContact.IsTouching())
                _hasWon.Value = true;
            if (NetworkManager.ServerTime.Time >= nextSnapshot) PublishFrame();
            _damageFeedback?.Observe(generation, egg.DamageCount, phase, _impactSequence);
            _fallTrajectory?.Present(egg.transform.position, egg.Body.linearVelocity,
                phase == NestEggPhase.Intact && !eggs.IsEggInNest && !egg.IsSupportedByNest);
        }
        else if (history.Sample(NetworkManager.ServerTime.Time - presentationDelay,
                     out NestEggFrame sample, _smoothOnlineCarry))
        {
            if (_predictCarryInputs && _carryPrediction.Generation != 0)
            {
                Pose predicted = _carryPrediction.Current.Pose;
                if (CanPredictCarry && _carryPrediction.PendingCount < NestCarryInputBuffer.Capacity)
                    predicted = _carryPrediction.Preview(_hostMovement.CarryPoint, _guestMovement.CarryPoint,
                        Mathf.Max(0f, Time.time - Time.fixedTime));
                sample = _reconciledPresentation.Present(sample, predicted, Time.deltaTime,
                    _clientCorrectionTime, _clientCorrectionSpeed, _clientCorrectionAngularSpeed);
            }
            else if (_smoothOnlineCarry)
            {
                Pose target = new Pose(sample.NestPosition, sample.NestRotation);
                bool anticipate = false;
                bool fresh = NetworkManager.ServerTime.Time - frame.Value.Time < 0.3;
                if (fresh && roundStage.Value == NestRoundStage.Playing &&
                    sample.Phase != NestEggPhase.Waiting &&
                    _hostMovement != null && _hostMovement.IsSpawned &&
                    _guestMovement != null && _guestMovement.IsSpawned &&
                    NestCarryPose.IsValidAxis(_guestMovement.CarryPoint - _hostMovement.CarryPoint))
                {
                    // Dùng điểm mang local ngay trong frame hiện tại, không chờ vòng khứ hồi.
                    target = carryPose.Calculate(_hostMovement.CarryPoint, _guestMovement.CarryPoint);
                    anticipate = true;
                }
                sample = _clientPresentation.Step(sample, target, _clientSmoothTime,
                    _maxPresentationOffset, _maxPresentationAngle, Time.deltaTime, anticipate,
                    _clientCorrectionTime, _clientCorrectionSpeed, _clientCorrectionAngularSpeed);
            }
            transform.SetPositionAndRotation(sample.NestPosition, sample.NestRotation);
            egg.ApplyRemote(sample);
            _damageFeedback?.Observe(sample.Generation, sample.EggDamage, sample.Phase, sample.ImpactSequence);
            _fallTrajectory?.Present(sample.EggPosition, sample.EggVelocity,
                sample.Phase == NestEggPhase.Intact && !sample.EggInNest && !sample.EggSupported);
            phase = sample.Phase;
        }
    }

    private void PublishFrame()
    {
        if (!IsServer || egg == null) return;
        if (_carryInputs.Generation != generation)
        {
            _carryInputs.Reset(generation);
            _serverCarryState = new NestCarryState { Position = body.position, Rotation = body.rotation };
            if (TryGetPlayers(out _, out NetworkObject guest) &&
                guest.TryGetComponent(out NetworkTestMovement movement))
                _lastGuestCarryAnchor = movement.CarryPoint;
        }
        double time = NetworkManager.ServerTime.Time;
        frame.Value = new NestEggFrame
        {
            Time = time,
            Generation = generation,
            Phase = phase,
            EggDamage = egg.DamageCount,
            ImpactSequence = _impactSequence,
            EggSupported = egg.IsSupportedByNest,
            EggInNest = eggs.IsEggInNest,
            AppliedCarrySequence = _predictCarryInputs ? _carryInputs.AppliedSequence : 0,
            CarryStepDuration = Time.fixedDeltaTime,
            NestVelocity = _serverCarryState.Velocity,
            NestPosition = body.position,
            NestRotation = body.rotation,
            EggPosition = egg.Body.position,
            EggVelocity = egg.Body.isKinematic ? Vector3.zero : egg.Body.linearVelocity,
            EggRotation = egg.Body.rotation
        };
        nextSnapshot = time + 1.0 / Mathf.Max(1, (int)NetworkManager.NetworkConfig.TickRate);
    }

    private void ReceiveFrame(NestEggFrame previous, NestEggFrame current)
    {
        if (!history.Add(current)) return;
        if (_predictCarryInputs && _carryPrediction != null)
        {
            if (_carryPrediction.Generation != current.Generation)
            {
                _carryPrediction.Reset(current);
                _reconciledPresentation.Reset(current);
            }
            else
            {
                Pose beforeReplay = _carryPrediction.Current.Pose;
                if (_carryPrediction.Reconcile(current))
                    _reconciledPresentation.Correct(beforeReplay, _carryPrediction.Current.Pose);
            }
        }
        if (history.Count == 1)
        {
            _clientPresentation.Reset(current);
            transform.SetPositionAndRotation(current.NestPosition, current.NestRotation);
            egg.ApplyRemote(current);
            phase = current.Phase;
        }
    }

    public void ResetEggOnServer()
    {
        if (!CanResetEgg || egg == null) return;
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest)) host = guest = null;
        eggs.Reset(eggDropClearance, host, guest);
        _eggRoundStart = NetworkManager.ServerTime.Time;
        phase = NestEggPhase.Intact;
        generation++;
        PublishFrame();
    }

    public void DropEggForTestOnServer()
    {
        if (!CanResetEgg || egg == null) return;
        eggs.DropForTest();
        phase = NestEggPhase.Intact;
        generation++;
        PublishFrame();
    }

    private void OnEggBroken()
    {
        if (!IsServer) return;
        phase = NestEggPhase.Broken;
        nextSnapshot = 0;
        RoundFailed?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        _hasWon.OnValueChanged -= OnWinChanged;
        WinChanged?.Invoke(false);
        frame.OnValueChanged -= ReceiveFrame;
        history.Clear();
        if (_cachePlayersRoutine != null) StopCoroutine(_cachePlayersRoutine);
        _cachePlayersRoutine = null;
        _hostMovement = _guestMovement = null;
        validSetup = placed = false;
        phase = NestEggPhase.Waiting;
        DestroyEgg();
    }

    private void DestroyEgg()
    {
        _fallTrajectory?.Dispose();
        _fallTrajectory = null;
        if (_damageFeedback != null)
        {
            _damageFeedback.enabled = false;
            Destroy(_damageFeedback.gameObject);
            _damageFeedback = null;
        }
        eggs?.Dispose();
        _eggCarryPhysics = null;
    }

    public override void OnDestroy()
    {
        DestroyEgg();
        base.OnDestroy();
    }
}
