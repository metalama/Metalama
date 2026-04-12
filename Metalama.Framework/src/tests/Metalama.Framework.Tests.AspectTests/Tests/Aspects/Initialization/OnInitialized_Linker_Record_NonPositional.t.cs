[TheAspect]
public record TargetRecord : IInitializable
{
  public int Value { get; init; }
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}
public class Caller
{
  public void Method()
  {
    var r1 = new TargetRecord
    {
      Value = 1
    }.WithInitialize();
    var r2 = (r1 with
    {
      Value = 2
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}