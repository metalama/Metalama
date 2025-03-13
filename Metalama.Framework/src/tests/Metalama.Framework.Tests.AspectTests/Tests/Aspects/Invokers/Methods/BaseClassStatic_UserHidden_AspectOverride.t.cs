public class TargetClass : BaseClass
{
  [OverrideAspect]
  public new static void Method()
  { // Invoke TargetClass.Method_Source
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Method_Source();
    // Invoke TargetClass.Method_Source
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Method_Source();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Method();
    // Invoke TargetClass.Method_Source
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Method_Source();
  }
  private static void Method_Source()
  {
  }
  [InvokerBeforeAspect]
  public void InvokerBefore()
  { // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method_Source
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method_Source
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    return;
  }
  [InvokerAfterAspect]
  public void InvokerAfter()
  { // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Method();
    // Invoke TargetClass.Method
    return;
  }
}