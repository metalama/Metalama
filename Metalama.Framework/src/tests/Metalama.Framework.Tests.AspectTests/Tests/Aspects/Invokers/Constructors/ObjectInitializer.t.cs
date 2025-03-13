public class TargetClass
{
  public int Field;
  public int Property { get; init; }
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke new <target>() { Field = 42, Property = 42 };
    var x = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.ObjectInitializer.TargetClass()
    {
      Field = 42,
      Property = 42
    };
    return;
  }
}