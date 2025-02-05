public class TargetClass : BaseClass
{
  public new static void Method()
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden.BaseClass.Method();
    return;
  }
}