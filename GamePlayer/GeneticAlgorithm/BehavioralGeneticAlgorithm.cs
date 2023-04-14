namespace GamePlayer.GeneticAlgorithm;

public class BehavioralGeneticAlgorithm
{
    public const int InputCount = 32;

    public BehavioralGeneticAlgorithm(GameStateReducer reducer)
    {
        _reducer = reducer;
    }

    public void Run()
    {

    }

    public abstract record Instruction();
    public record CopyInputToAccumulator(int Input)
    {
        public static CopyInputToAccumulator Random => new(System.Random.Shared.Next(InputCount));
    }

    public record Add(int Value)
    {
        public static Add Random => new (System.Random.Shared.Next(int.MinValue, int.MaxValue));
    }

    public record AddInput(int Input)
    {
        public static AddInput Random => new(System.Random.Shared.Next(InputCount));
    }

    public record SubInput(int Input)
    {
        public static SubInput Random => new(System.Random.Shared.Next(InputCount));
    }

    public record CopyAccumulatorToInput(int Input)
    {
        public static CopyAccumulatorToInput Random => new(System.Random.Shared.Next(InputCount));
    }

    public record 
}
