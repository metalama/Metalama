[FirstAspect]
[SecondAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Second1");
    Console.WriteLine("Second2");
    Console.WriteLine("First1");
    Console.WriteLine("First2");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context);
    Console.WriteLine("Second1");
    Console.WriteLine("Second2");
    Console.WriteLine("First1");
    Console.WriteLine("First2");
  }
}