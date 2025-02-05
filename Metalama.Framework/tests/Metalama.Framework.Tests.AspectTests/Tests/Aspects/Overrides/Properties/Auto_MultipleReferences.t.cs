internal class TargetClass
{
  private int _property;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      if (this._property < 0)
      {
        this._property = 0;
      }
      return this._property;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      var current = this._property;
      this._property = current + 1;
    }
  }
  private static int _staticProperty;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      if (global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences.TargetClass._staticProperty < 0)
      {
        global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences.TargetClass._staticProperty = 0;
      }
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences.TargetClass._staticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      var current = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences.TargetClass._staticProperty;
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences.TargetClass._staticProperty = current + 1;
    }
  }
}