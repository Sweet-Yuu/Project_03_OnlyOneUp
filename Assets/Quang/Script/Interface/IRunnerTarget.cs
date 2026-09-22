using UnityEngine;

public interface IRunnerTarget
{
    Vector3 Position {get;}
    bool IsActive {get;}
    public bool IsTripped {get;}
}
