namespace GamePlayer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        const bool replay = false;

        var levelData = LevelData.Load("level.dat");
        var level = levelData[0];
        var reducer = new GameStateReducer(level);

        IEnumerable<InputState> bestInputs;

        if (replay)
        {
            bestInputs =
                InputStateSerializer.Deserialize(Convert.FromBase64String(File.ReadAllText("bestGeneration.txt")));
        }
        else
        {
            var captureGenerations = new[] { 1, 10, 100, 1000, 5000, 10000 };

            File.WriteAllText("accuracy.csv", "0");
            var learner = new GeneticAlgorithm.GeneticAlgorithm(reducer);

            bestInputs = Array.Empty<InputState>();

            learner.GenerationCompleted += (_, generation) =>
            {
                Console.WriteLine($"{generation.Generation} ({generation.BestInputs.Length}): {generation.MaxScore}");

                File.AppendAllText("accuracy.csv", $",{generation.MaxScore}");

                if (captureGenerations.Contains(generation.Generation))
                    WriteGeneration(generation.BestInputs, generation.Generation);

                generation.ShouldContinue = generation.MaxScore < 200 && generation.Generation < 1000;
                bestInputs = generation.BestInputs;
            };

            learner.Run();

            var base64Data = Convert.ToBase64String(InputStateSerializer.Serialize(bestInputs).ToArray());
            File.WriteAllText("bestGeneration.txt", base64Data);
        }

        new GameView(level, bestInputs, false).Run();
    }

    private static void WriteGeneration(InputState[] inputs, int generation)
    {
        var base64Data = Convert.ToBase64String(InputStateSerializer.Serialize(inputs).ToArray());
        File.WriteAllText($"bestGeneration-{generation}.txt", base64Data);
    }
}

