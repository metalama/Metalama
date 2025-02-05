public class TargetClass
{
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke new <target>();
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Simple.TargetClass();
    return;
  }
}