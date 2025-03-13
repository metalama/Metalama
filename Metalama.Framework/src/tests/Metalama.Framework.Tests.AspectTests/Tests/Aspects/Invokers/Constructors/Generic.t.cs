public class TargetClass<T>
{
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke new <target><T>();
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Generic.TargetClass<T>();
    // Invoke new <target><int>();
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Generic.TargetClass<global::System.Int32>();
    return;
  }
}