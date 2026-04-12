[TheAspect]
public sealed class TargetCode : IInitializable
{
  public void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}