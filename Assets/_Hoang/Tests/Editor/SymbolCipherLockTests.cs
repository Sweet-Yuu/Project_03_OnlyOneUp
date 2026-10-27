using System;
using Hoang.Puzzles;

namespace Hoang.Tests
{
    public static class SymbolCipherLockTests
    {
        public static int Main()
        {
            try
            {
                var puzzle = new SymbolCipherLock(7, 5, 8);
                Check(puzzle.Code == "523", "The three clues must decode to circle=5, triangle=2, square=3.");
                Check(!puzzle.IsUnlocked, "The path starts locked.");
                foreach (string entry in new[] { null, "", "52", "0523", "235", " 523", "5a3", "５２３" })
                    Check(!puzzle.TryUnlock(entry) && !puzzle.IsUnlocked, "Malformed or incorrect code must keep the path locked.");
                Check(puzzle.TryUnlock("523") && puzzle.IsUnlocked, "The exact solution must unlock the path.");
                puzzle.TryUnlock("000");
                Check(puzzle.IsUnlocked, "A later wrong guess must not relock a solved puzzle.");

                var leadingZero = new SymbolCipherLock(2, 5, 3);
                Check(leadingZero.Code == "023", "Leading zero belongs to the code.");
                Check(!leadingZero.TryUnlock("23") && leadingZero.TryUnlock("023"), "Leading zero must be entered.");
                Check(new SymbolCipherLock(0, 0, 0).TryUnlock("000"), "Zero is a valid symbol value.");
                Check(new SymbolCipherLock(18, 18, 18).TryUnlock("999"), "Nine is a valid symbol value.");

                foreach (int[] sums in new[] {
                    new[] { -1, 5, 8 }, new[] { 7, 19, 8 }, new[] { 7, 5, int.MaxValue },
                    new[] { 7, 5, 7 }, new[] { 1, 8, 1 }, new[] { 18, 0, 0 }, new[] { 18, 2, 4 }
                })
                {
                    bool rejected = false;
                    try { new SymbolCipherLock(sums[0], sums[1], sums[2]); }
                    catch (ArgumentException) { rejected = true; }
                    Check(rejected, "Invalid or non-digit solutions must be rejected.");
                }
                Console.WriteLine("PASS: symbol cipher solution, retry, latch, input validation, digit boundaries and invalid clues.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
