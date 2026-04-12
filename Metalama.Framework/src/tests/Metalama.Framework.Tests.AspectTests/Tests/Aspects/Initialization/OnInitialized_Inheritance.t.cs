[TheAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("Initialized BaseClass");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context);
    Console.WriteLine("Initialized DerivedClass");
  }
}