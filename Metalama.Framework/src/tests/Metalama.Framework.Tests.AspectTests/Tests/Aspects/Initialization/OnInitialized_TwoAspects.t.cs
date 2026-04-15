[FirstAspect]
[SecondAspect]
public class TargetCode : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Second1");
    Console.WriteLine("Second2");
    Console.WriteLine("First1");
    Console.WriteLine("First2");
  }
}