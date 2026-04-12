[TheAspect]
public record BaseRecord(int X) : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}
public record DerivedRecord(int X, int Y) : BaseRecord(X);
public class Caller
{
  public void Method()
  {
    var d = new DerivedRecord(1, 2).WithInitialize();
    var d2 = (d with
    {
      Y = 5
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}