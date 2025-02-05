public class TargetClass : BaseClass
{
  public new static int Property
  {
    get
    {
      return 0;
    }
    set
    {
    }
  }
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property;
      return 0;
    }
    set
    { // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden.BaseClass.Property = 42;
    }
  }
}