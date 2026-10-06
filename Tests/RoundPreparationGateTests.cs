using System;

// Standalone tests; compile with RoundPreparationGate.cs, then run the executable.
internal static class RoundPreparationGateTests
{
    private static void Expect(NestRoundStage actual, NestRoundStage expected, string name)
    {
        if (actual != expected) throw new Exception(name + ": " + actual + " != " + expected);
    }

    public static void Main()
    {
        var gate = new RoundPreparationGate();
        gate.Begin(0f);
        Expect(gate.Step(1f, 1f, false, true, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Elapsed time alone must not release egg without acknowledgements/positions");
        Expect(gate.Step(2f, 1f, true, false, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Moving players cannot start");
        Expect(gate.Step(2.1f, .1f, true, true, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Not enough continuous stability");
        Expect(gate.Step(2.2f, .1f, false, true, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Readiness loss resets stability");
        Expect(gate.Step(2.3f, .1f, true, true, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Must accumulate again after reset");
        Expect(gate.Step(2.41f, .11f, true, true, .2f, 15f), NestRoundStage.PlacingNest,
            "Stable players place tray but do not release egg in same step");
        Expect(gate.Step(2.43f, .02f, true, false, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Movement during placement cancels release");
        Expect(gate.Step(2.7f, .27f, true, true, .2f, 15f), NestRoundStage.PlacingNest, "Place again");
        Expect(gate.Step(2.72f, .02f, true, true, .2f, 15f), NestRoundStage.Playing,
            "Release only on following physics step");
        Expect(gate.Step(30f, .02f, false, false, .2f, 15f), NestRoundStage.Playing,
            "Normal movement after release does not restart setup");
        gate.Begin(40f);
        Expect(gate.Step(56f, .02f, false, true, .2f, 15f), NestRoundStage.SetupFailed,
            "Timeout fails closed");
        Expect(gate.Step(57f, 1f, true, true, .2f, 15f), NestRoundStage.SetupFailed,
            "Late readiness cannot release failed attempt");
        gate.Begin(60f);
        Expect(gate.Step(60.1f, .1f, true, true, .2f, 15f), NestRoundStage.PreparingPlayers,
            "Retry clears previous stability");
        Console.WriteLine("PASS: 13 round preparation assertions.");
    }
}
