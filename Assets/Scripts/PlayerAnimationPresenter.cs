using UnityEngine;

// Presentation only. Network state is supplied by NetworkTestMovement.
public sealed class PlayerAnimationPresenter
{
    private readonly Animator animator;
    private readonly Object context;
    private static readonly int Idle = Animator.StringToHash("Base Layer.IdleCarrying");
    private static readonly int Walk = Animator.StringToHash("Base Layer.Walk");

    public PlayerAnimationPresenter(Animator animator, Object context)
    {
        this.animator = animator;
        this.context = context;
        if (animator != null) animator.applyRootMotion = false;
    }

    public void Validate()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            Debug.LogWarning("Assign Carrying's Animator and its IdleCarrying/Walk controller for online animation.", context);
        else if (!animator.HasState(0, Idle) || !animator.HasState(0, Walk))
            Debug.LogWarning("Online Animator needs Base Layer states named IdleCarrying and Walk.", context);
    }

    public void Show(OnlineMovementState state, float fade)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int hash = state == OnlineMovementState.Walk ? Walk : Idle;
        if (animator.HasState(0, hash)) animator.CrossFadeInFixedTime(hash, fade, 0);
    }
}
