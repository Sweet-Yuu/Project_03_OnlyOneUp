public enum NestRoundStage : byte { WaitingForPlayers, PreparingPlayers, PlacingNest, Playing, SetupFailed }

// Pure state machine: readiness must hold continuously, and placing the nest
// and releasing the egg must occur on separate physics steps.
public sealed class RoundPreparationGate
{
    public NestRoundStage Stage { get; private set; }
    private float startedAt;
    private float stableFor;

    public void Begin(float now)
    {
        startedAt = now;
        stableFor = 0f;
        Stage = NestRoundStage.PreparingPlayers;
    }

    public NestRoundStage Step(float now, float stepDuration, bool ready, bool still,
        float requiredStableDuration, float timeout)
    {
        if (Stage == NestRoundStage.Playing || Stage == NestRoundStage.SetupFailed) return Stage;
        if (now - startedAt > timeout) return Stage = NestRoundStage.SetupFailed;
        if (!ready || !still)
        {
            stableFor = 0f;
            return Stage = NestRoundStage.PreparingPlayers;
        }
        if (Stage == NestRoundStage.PlacingNest) return Stage = NestRoundStage.Playing;
        stableFor += stepDuration;
        if (stableFor >= requiredStableDuration) Stage = NestRoundStage.PlacingNest;
        return Stage;
    }
}
