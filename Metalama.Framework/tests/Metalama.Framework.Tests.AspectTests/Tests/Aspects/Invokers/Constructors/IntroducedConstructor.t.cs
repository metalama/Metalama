public class TargetClass
{
  [InvokerAspect]
  public void Invoker()
  { // Invoke new <target>(42);
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.IntroducedConstructor.TargetClass(42);
    return;
  }
  public TargetClass(global::System.Int32 x)
  {
  }
}