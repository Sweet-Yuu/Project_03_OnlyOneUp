using System;
using Hoang.Puzzles;

namespace Hoang.Tests
{
    public static class LinkedLightsBoardTests
    {
        public static int Main()
        {
            try
            {
                var board = new LinkedLightsBoard(new[] { 1, 3, 5, 7, 9 });
                Check(board.Mask == 511 && !board.IsSolved, "The five known presses must produce all nine lights on.");
                Check(board.TryPress(1) && board.Mask == 500, "Top-left press must toggle only buttons 1, 2 and 4.");
                Check(!board.IsLit(1) && !board.IsLit(2) && !board.IsLit(4) && board.IsLit(3) && board.IsLit(5),
                    "Corner press must not wrap around rows or toggle a diagonal.");
                Check(board.TryPress(1) && board.Mask == 511, "Pressing the same button twice must undo it.");
                Check(board.TryPress(3) && board.Mask == 473, "Top-right press must not wrap into button 4.");
                Check(board.TryReset() && board.Mask == 511, "Reset must restore the exact initial board.");
                Check(board.TryPress(5) && board.Mask == 325, "Center press must leave only the four corners lit.");
                Check(!board.TryPress(0) && !board.TryPress(10) && board.Mask == 325, "Out-of-range presses must leave the board unchanged.");
                bool invalidRead = false;
                try { board.IsLit(0); } catch (ArgumentOutOfRangeException) { invalidRead = true; }
                Check(invalidRead, "Invalid light reads must be rejected.");

                var defaultBoard = new LinkedLightsBoard(new[] { 1, 2, 4, 5, 9 });
                Check(defaultBoard.Mask == 351, "The documented default board must have the hand-checked mask 351.");
                foreach (int button in new[] { 1, 2, 4, 5, 9 }) Check(defaultBoard.TryPress(button), "Default solution presses must be accepted.");
                Check(defaultBoard.Mask == 0 && defaultBoard.IsSolved, "The inverse scramble sequence must solve the board.");
                Check(!defaultBoard.TryReset() && !defaultBoard.TryPress(1) && defaultBoard.Mask == 0,
                    "Solved boards must keep the path unlocked even after reset or another press.");

                foreach (int[] seed in new[] { null, new int[0], new[] { 0 }, new[] { 10 }, new[] { 1, 1 } })
                {
                    bool rejected = false;
                    try { new LinkedLightsBoard(seed); } catch (ArgumentException) { rejected = true; }
                    Check(rejected, "Null, invalid or already-solved scrambles must be rejected.");
                }
                int[] mutableSeed = { 1, 2, 4, 5, 9 };
                var independent = new LinkedLightsBoard(mutableSeed);
                mutableSeed[0] = 9;
                independent.TryPress(3);
                Check(independent.TryReset() && independent.Mask == 351, "Changing the source array must not change the saved initial layout.");
                Console.WriteLine("PASS: linked lights neighbors, no wrap/diagonals, inverse presses, solution, reset, solved latch and invalid seeds.");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
