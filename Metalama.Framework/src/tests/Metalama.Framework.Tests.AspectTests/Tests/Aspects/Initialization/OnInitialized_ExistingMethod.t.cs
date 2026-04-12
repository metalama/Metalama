[TheAspect]
public class TargetCode : IInitializable
{
  public virtual void Initialize(InitializationContext context)
  {
    Console.WriteLine("From aspect!");
    Console.WriteLine("Hand-authored!");
  }
}