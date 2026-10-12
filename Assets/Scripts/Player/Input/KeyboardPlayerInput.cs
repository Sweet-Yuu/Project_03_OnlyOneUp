using UnityEngine;
using UnityEngine.InputSystem;

public readonly struct OnlinePlayerInput
{
    public readonly Vector2 Move;
    public readonly bool Jump;
    public OnlinePlayerInput(Vector2 move, bool jump) { Move = move; Jump = jump; }
}

public interface IOnlinePlayerInput
{
    OnlinePlayerInput Read();
}

public sealed class KeyboardPlayerInput : IOnlinePlayerInput
{
    public OnlinePlayerInput Read()
    {
        Keyboard keys = Keyboard.current;
        if (!Application.isFocused || keys == null) return new OnlinePlayerInput(Vector2.zero, false);
        Vector2 input = new Vector2(
            (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f)
                - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f),
            (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f)
                - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f));
        return new OnlinePlayerInput(input, keys.spaceKey.wasPressedThisFrame);
    }
}
