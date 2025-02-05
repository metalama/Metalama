public class TargetClass : BaseClass
{
  public new static int Field;
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field;
      // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field;
      // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field;
      // Invoke BaseClass.Field
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field;
      return 0;
    }
    set
    { // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field = 42;
      // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field = 42;
      // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field = 42;
      // Invoke BaseClass.Field
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_UserHidden.BaseClass.Field = 42;
    }
  }
}