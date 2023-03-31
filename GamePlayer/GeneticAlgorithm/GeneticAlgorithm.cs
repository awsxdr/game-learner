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
    private const int PopulationSize = 2500;
    private const int Diversity = 10;
    private const int MinimumCrossovers = 1;
    private const int MaximumCrossovers = 100;
    private const int StartGeneCount = 10;
    private const int GeneGrowth = 10;
    private const int GeneGrowthInterval = 5;
    private const int ForcedGeneGrowthInterval = 50;
    private const double GrowthImprovementRequirement = 2.0;

    private int _mutationRate = 10000;
    private double _lastGrowthScore = -100;

    private static readonly Random Random = new();

    private static readonly GameState InitialState =
        new(new(new Vector2(5.0f, 10.0f), new Vector2(0.0f, 0.0f), false), 16f, true);

    public event EventHandler<GenerationCompletedEventArgs>? GenerationCompleted;

    public GeneticAlgorithm(GameStateReducer reducer)
    {
        _reducer = reducer;
    }

    public void Run()
    {
        GenerationCompletedEventArgs generationCompletedEventArgs;

        var population = GetRandomPopulation();

        var generation = 0;
        var lastBest = 0f;

        do
        {
            var scoredPopulation = ScorePopulation(population).ToArray();
            scoredPopulation = scoredPopulation.OrderByDescending(x => x.Score).ToArray();

            generationCompletedEventArgs = new GenerationCompletedEventArgs(scoredPopulation[0].Score, ++generation, scoredPopulation[0].Inputs.ToArray());

            var combinations = scoredPopulation.Take(Diversity).GetCombinations().ToArray();

            population = combinations
                .AsParallel()
                .SelectMany(x => Enumerable.Range(0, PopulationSize / combinations.Length)
                    .AsParallel()
                    .Select(_ => BreedSequence(x.First.Inputs, x.Second.Inputs))
                    .ToArray())
                .ToArray();

            GenerationCompleted?.Invoke(this, generationCompletedEventArgs);

            if (lastBest >= generationCompletedEventArgs.MaxScore)
            {
                _mutationRate = Math.Max(10, _mutationRate / 2);
            }
            else
            {
                _mutationRate = Math.Min(10000, _mutationRate * 2);
            }

            if (
                generation % ForcedGeneGrowthInterval == 0
                || (generation % GeneGrowthInterval == 0 && generationCompletedEventArgs.MaxScore - _lastGrowthScore > GrowthImprovementRequirement))
            {
                population = population.Select(genes => genes.Concat(GetRandomGenes(GeneGrowth)).ToArray()).ToArray();
                _lastGrowthScore = generationCompletedEventArgs.MaxScore;
            }

            lastBest = generationCompletedEventArgs.MaxScore;

        } while (generationCompletedEventArgs.ShouldContinue);
    }

    private static IEnumerable<InputState> BreedSequence(IEnumerable<InputState> parent1, IEnumerable<InputState> parent2)
    {
        if (Random.Next(0, 2) == 0)
        {
            parent1 = parent1.ToArray();
            parent2 = parent2.ToArray();
        }
        else
        {
            parent1 = parent2.ToArray();
            parent2 = parent1.ToArray();
        }

        var crossovers = Enumerable.Range(0, Random.Next(MinimumCrossovers, MaximumCrossovers))
            .Select(_ => Random.Next(0, Math.Min(parent1.Count(), parent2.Count())))
            .OrderBy(x => x)
            .Distinct()
            .ToArray();

        return parent1.Zip(parent2)
            .Select((p, i) =>
                crossovers.Count(c => c < i) % 2 == 0
                    ? p.First
                    : p.Second)
            .Select(i => Random.Next(0, 10000) == 0 ? GetRandomInputState() : i)
            .ToArray();
    }

    private static IEnumerable<IEnumerable<InputState>> GetRandomPopulation() =>
        Enumerable.Range(0, PopulationSize)
            .Select(_ => GetRandomGenes(StartGeneCount));

    private static IEnumerable<InputState> GetRandomGenes(int length) =>
        Enumerable.Range(0, length).Select(_ => GetRandomInputState());

    private static InputState GetRandomInputState() =>
        new(
            (LeftRightStatus) Random.Next(0, 0b11),
            Random.Next(0, 2) == 1);

    private IEnumerable<(float Score, IEnumerable<InputState> Inputs)> ScorePopulation(
        IEnumerable<IEnumerable<InputState>> population)
        =>
            population
                .AsParallel()
                .WithDegreeOfParallelism(8)
                .Select(ScoreInput)
                .ToArray();

    private (float Score, IEnumerable<InputState> Inputs) ScoreInput(IEnumerable<InputState> inputs)
    {
        inputs = inputs.ToArray();
        return (ScoreState(inputs.Aggregate((State: InitialState, EndReachGeneration: 0, Generation: 0), (state, input) =>
        {
            var resultState = _reducer.Reduce(state.State, input);

            return (
                resultState, 
                state.EndReachGeneration > 0 ? state.EndReachGeneration 
                : resultState.CharacterDetails.Position.X >= 200 ? state.Generation
                : 0,
                state.Generation + 1);
        })), inputs);
    }

    private static float ScoreState((GameState State, int EndReachGeneration, int _) values) => 
        (Math.Min(200, values.State.CharacterDetails.Position.X) + (values.EndReachGeneration > 0 ? (100f / values.EndReachGeneration) : 0))
        / (values.State.IsAlive ? 1.0f : 2.0f);
}