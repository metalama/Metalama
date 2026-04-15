[FirstAspect]
[SecondAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("First.BeforeBase");
    Console.WriteLine("Second.BeforeBase");
    Console.WriteLine("Second.AfterBase");
    Console.WriteLine("First.AfterBase");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("First.BeforeBase");
    Console.WriteLine("Second.BeforeBase");
    base.Initialize(context);
    Console.WriteLine("Second.AfterBase");
    Console.WriteLine("First.AfterBase");
  }
}