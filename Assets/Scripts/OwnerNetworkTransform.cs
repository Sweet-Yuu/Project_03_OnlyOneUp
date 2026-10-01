using Unity.Netcode.Components;

// Prototype: each player publishes their own movement to the host and peers.
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
