public class TargetClass : BaseClass
{
  private static global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.OverrideAspect]
  public static new global::System.Int32 Field
  {
    get
    {
      // Invoke TargetClass._field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field;
      // Invoke TargetClass._field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Field;
      // Invoke TargetClass._field
      return global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field;
    }
    set
    {
      // Invoke TargetClass._field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field = 42;
      // Invoke TargetClass._field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Field = 42;
      // Invoke TargetClass._field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.TargetClass._field = value;
    }
  }
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass._field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass._field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      return 0;
    }
    set
    { // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass._field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass._field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field;
      // Invoke TargetClass.Field
      return 0;
    }
    set
    { // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
      // Invoke TargetClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Field = 42;
    }
  }
}