using System;
using System.Diagnostics;
using System.Linq;
using OpenTK.Mathematics;

namespace GamePlayer;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    //[STAThread]
    static void Main()
    {
        var levelData = LevelData.Load("level.dat");
        var reducer = new GameStateReducer(levelData);

        var learner = new GeneticAlgorithm.GeneticAlgorithm(reducer);

        var bestInputs = Array.Empty<InputState>();

        learner.GenerationCompleted += (_, generation) =>
        {
            Console.WriteLine($"{generation.Generation}: {generation.MaxScore}");

            generation.ShouldContinue = generation.Generation < 100;
            bestInputs = generation.BestInputs;
        };

        learner.Run();

        new GameView(levelData, bestInputs).Run();
    }
}

