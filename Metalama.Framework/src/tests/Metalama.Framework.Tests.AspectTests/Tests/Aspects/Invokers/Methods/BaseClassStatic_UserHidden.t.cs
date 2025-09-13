public class TargetClass : BaseClass
{
  public static new void Method()
  {
  }
  [InvokerAspect]
  public void Invoker()
  { // Invoke BaseClass.Method
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