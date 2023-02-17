using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace GamePlayer.GeneticAlgorithm;

public class GenerationCompletedEventArgs : EventArgs
{
    public float MaxScore { get; }
    public int Generation { get; }
    public InputState[] BestInputs { get; }
    public bool ShouldContinue { get; set; } = true;

    public GenerationCompletedEventArgs(float maxScore, int generation, InputState[] bestInputs)
    {
        MaxScore = maxScore;
        Generation = generation;
        BestInputs = bestInputs;
    }
}

public class GeneticAlgorithm
{
    private readonly GameStateReducer _reducer;
    private const int PopulationSize = 1000;

    private static readonly Random Random = new();

    private static readonly GameState InitialState =
        new(new(new Vector2(5.0f, 10.0f), new Vector2(0.0f, 0.0f), false), 16f);

    public event EventHandler<GenerationCompletedEventArgs>? GenerationCompleted;

    public GeneticAlgorithm(GameStateReducer reducer)
    {
        _reducer = reducer;

        var ones = Enumerable.Repeat(1, 100);
        var zeroes = Enumerable.Repeat(0, 100);
        
        var test = BreedSequence(ones, zeroes).ToArray();
    }

    public void Run()
    {
        GenerationCompletedEventArgs generationCompletedEventArgs;

        var population = GetRandomPopulation();

        var generation = 0;

        do
        {
            var scoredPopulation = ScorePopulation(population).OrderByDescending(x => x.Score).ToArray();

            generationCompletedEventArgs = new GenerationCompletedEventArgs(scoredPopulation[0].Score, ++generation, scoredPopulation[0].Inputs.ToArray());

            population = scoredPopulation.Take(5).GetCombinations()
                .SelectMany(x => Enumerable.Range(0, 100)
                    .Select(_ => BreedSequence(x.First.Inputs, x.Second.Inputs))
                    .ToArray())
                .ToArray();

            GenerationCompleted?.Invoke(this, generationCompletedEventArgs);

        } while (generationCompletedEventArgs.ShouldContinue);
    }

    private static IEnumerable<T> BreedSequence<T>(IEnumerable<T> parent1, IEnumerable<T> parent2)
    {
        parent1 = parent1.ToArray();
        parent2 = parent2.ToArray();

        var crossovers = Enumerable.Range(0, Random.Next(3, 7))
            .Select(_ => Random.Next(0, Math.Min(parent1.Count(), parent2.Count())))
            .OrderBy(x => x)
            .Distinct()
            .ToArray();

        return parent1.Zip(parent2)
            .Select((p, i) =>
                crossovers.Count(c => c < i) % 2 == 0
                    ? p.First
                    : p.Second)
            .ToArray();
    }

    private static IEnumerable<IEnumerable<InputState>> GetRandomPopulation() =>
        Enumerable.Range(0, PopulationSize)
            .Select(_ => Enumerable.Range(0, Random.Next(900, 1100)).Select(_ => GetRandomInputState()));

    private static InputState GetRandomInputState() =>
        new(
            (LeftRightStatus) Random.Next(0, 0b11),
            Random.Next(0, 2) == 1);

    private IEnumerable<(float Score, IEnumerable<InputState> Inputs)> ScorePopulation(
        IEnumerable<IEnumerable<InputState>> population)
        =>
            population
                .Select(ScoreInput);

    private (float Score, IEnumerable<InputState> Inputs) ScoreInput(IEnumerable<InputState> inputs) =>
        (ScoreState(inputs.Aggregate(InitialState, _reducer.Reduce)), inputs);

    private static float ScoreState(GameState state) => state.CharacterDetails.Position.X;
}