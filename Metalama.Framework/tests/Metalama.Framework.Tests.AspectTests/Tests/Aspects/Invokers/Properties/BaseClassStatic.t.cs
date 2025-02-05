public class TargetClass : BaseClass
{
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property;
      return 0;
    }
    set
    { // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic.BaseClass.Property = 42;
    }
  }
}