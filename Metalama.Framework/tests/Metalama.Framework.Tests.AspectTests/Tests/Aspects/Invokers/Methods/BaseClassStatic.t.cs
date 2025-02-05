public class TargetClass : BaseClass
{
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic.BaseClass.Method();
    return;
  }
}