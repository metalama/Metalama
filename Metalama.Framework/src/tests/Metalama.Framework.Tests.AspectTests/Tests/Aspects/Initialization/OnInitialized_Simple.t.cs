[TheAspect]
public class TargetCode : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized!");
  }
}