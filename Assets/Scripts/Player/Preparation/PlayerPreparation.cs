using UnityEngine;

// Tracks the setup handshake; transport and authority checks stay in the NetworkBehaviour.
public sealed class PlayerPreparation
{
    private uint requested;
    private uint acknowledged;
    private Vector3 target;
    private bool responded;
    private bool accepted;
    private string details;
    public uint LocalSequence { get; private set; }
    public bool LocalPlacementSucceeded { get; private set; }

    public void Reset()
    {
        requested = acknowledged = LocalSequence = 0;
        responded = accepted = LocalPlacementSucceeded = false;
        target = Vector3.zero;
        details = null;
    }

    public void Begin(uint sequence, Vector3 position)
    {
        requested = sequence;
        acknowledged = 0;
        responded = accepted = false;
        details = "Awaiting owner reply";
        target = position;
    }

    public bool TryBeginLocal(uint sequence)
    {
        if (sequence <= LocalSequence) return false;
        LocalSequence = sequence;
        LocalPlacementSucceeded = false;
        return true;
    }

    public void CompleteLocal(bool success) => LocalPlacementSucceeded = success;

    public void Confirm(uint sequence, bool success, string ownerDetails)
    {
        if (sequence != requested) return;
        responded = true;
        accepted = success;
        details = ownerDetails;
        if (success) acknowledged = sequence;
    }

    public bool Rejected(uint sequence) => requested == sequence && responded && !accepted;
    public bool Ready(uint sequence, Vector3 position) => sequence == requested &&
        sequence == acknowledged && Vector3.Distance(position, target) < 0.15f;

    public string Describe(ulong ownerId, uint sequence, Vector3 position) =>
        $"player {ownerId}: request={requested}, expected={sequence}, " +
        $"response={responded}, accepted={accepted}, " +
        $"serverPosition={position:F3}, target={target:F3}, " +
        $"serverError={Vector3.Distance(position, target):F3}; {details}";
}
