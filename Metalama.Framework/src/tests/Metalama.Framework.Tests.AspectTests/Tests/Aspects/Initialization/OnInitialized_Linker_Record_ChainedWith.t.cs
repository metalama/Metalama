[TheAspect]
public record TargetRecord(int A, int B) : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}
public class Caller
{
  public void Method()
  {
    var r1 = new TargetRecord(1, 2).WithInitialize();
    var r2 = ((r1 with
    {
      A = 10
    }
    ).WithInitialize(InitializationMetadata.Modify)with
    {
      B = 20
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}