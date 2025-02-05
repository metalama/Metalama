[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field;
      // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field;
      // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field;
      return 0;
    }
    set
    { // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field = 42;
      // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field = 42;
      // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.BaseClass.Field = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field;
      return 0;
    }
    set
    { // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden.TargetClass.Field = 42;
    }
  }
  public static new global::System.Int32 Field;
}