[Introduction]
internal class TargetClass
{
  private int _property = 42;
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
  private static int _staticProperty = 42;
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass._staticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass._staticProperty = value;
    }
  }
  private global::System.Int32 _introducedProperty = (global::System.Int32)global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass.StaticProperty;
  public global::System.Int32 IntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._introducedProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedProperty = value;
    }
  }
  private static global::System.Int32 _introducedStaticProperty = (global::System.Int32)global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass.StaticProperty;
  public static global::System.Int32 IntroducedStaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass._introducedStaticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers.TargetClass._introducedStaticProperty = value;
    }
  }
}