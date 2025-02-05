[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public void InvokerBefore()
  { // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    return;
  }
  [InvokerAfterAspect]
  public void InvokerAfter()
  { // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    return;
  }
  public static new void Method()
  {
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.TargetClass.Method();
    // Invoke BaseClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden.BaseClass.Method();
  }
}