[TheAspect]
public record TargetRecord(int X) : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public int Y { get; init; } = X * 2;
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
  }
}