[Introduction]
internal class TargetClass
{
  private global::System.Int32 _field = 42;
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._field = value;
    }
  }
  private static global::System.Int32 _staticField = 42;
  public static global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass._staticField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass._staticField = value;
    }
  }
  static TargetClass()
  {
    StaticField = 27;
  }
  public TargetClass()
  {
    Field = 27;
  }
  private global::System.Int32 _introducedField = (global::System.Int32)global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass.StaticField;
  public global::System.Int32 IntroducedField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._introducedField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedField = value;
    }
  }
  private static global::System.Int32 _introducedStaticField = (global::System.Int32)global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass.StaticField;
  public static global::System.Int32 IntroducedStaticField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass._introducedStaticField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Initializers.TargetClass._introducedStaticField = value;
    }
  }
}