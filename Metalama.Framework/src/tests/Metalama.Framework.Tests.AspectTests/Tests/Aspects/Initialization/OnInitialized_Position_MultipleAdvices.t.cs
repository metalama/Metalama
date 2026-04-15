[TheAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    base.Initialize(context);
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
  }
}