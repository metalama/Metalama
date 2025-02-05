internal class TargetClass
{
  private int _property;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._property;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._property = value;
    }
  }
  private static int _staticProperty;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto.TargetClass._staticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto.TargetClass._staticProperty = value;
    }
  }
  private readonly int _propertyInitOnly;
  [Override]
  public int PropertyInitOnly
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._propertyInitOnly;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._propertyInitOnly = value;
    }
  }
  public int __Init
  {
    init
    {
      // Init-only setter should be accessible from other init-only setters.
      PropertyInitOnly = 42;
    }
  }
}