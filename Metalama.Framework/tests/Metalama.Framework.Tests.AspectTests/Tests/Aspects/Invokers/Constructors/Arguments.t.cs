public class TargetClass
{
  public TargetClass(int x, object y)
  {
  }
  [InvokerAspect]
  public void Invoker()
  { // Invoke new <target>();
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Arguments.TargetClass(42, new object ());
    return;
  }
}