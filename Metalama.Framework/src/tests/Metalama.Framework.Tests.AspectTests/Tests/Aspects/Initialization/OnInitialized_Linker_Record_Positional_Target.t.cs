[TheAspect]
public record TargetRecord(int X) : IInitializable
{
  public int Y { get; init; } = X * 2;
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}