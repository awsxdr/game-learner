using System;

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