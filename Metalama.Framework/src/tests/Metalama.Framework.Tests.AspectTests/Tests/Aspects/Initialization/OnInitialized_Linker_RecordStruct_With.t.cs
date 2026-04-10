[TheAspect]
public record struct TargetRecordStruct(int Value) : IInitializable
{
  public void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}
public class Caller
{
  public void Method()
  {
    var r1 = new TargetRecordStruct(1).WithInitialize();
    var r2 = (r1 with
    {
      Value = 2
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}