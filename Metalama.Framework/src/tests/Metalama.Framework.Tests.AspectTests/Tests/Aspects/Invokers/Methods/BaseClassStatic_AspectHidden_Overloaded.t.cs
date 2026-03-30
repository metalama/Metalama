[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public void InvokerBefore()
  {
    // Invoke TargetClass.Method()
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.TargetClass.Method();
    // Invoke BaseClass.Method()
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.BaseClass.Method();
    // Invoke TargetClass.Method()
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.TargetClass.Method();
    return;
  }
  [InvokerAfterAspect]
  public void InvokerAfter()
  {
    // Invoke BaseClass.Method(int) - not hidden, should stay on BaseClass
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.BaseClass.Method(42);
    // Invoke BaseClass.Method(int)
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.BaseClass.Method(42);
    // Invoke BaseClass.Method(int)
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.BaseClass.Method(42);
    return;
  }
  public static new void Method()
  {
    // Invoke BaseClass.Method()
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded.BaseClass.Method();
  }
}