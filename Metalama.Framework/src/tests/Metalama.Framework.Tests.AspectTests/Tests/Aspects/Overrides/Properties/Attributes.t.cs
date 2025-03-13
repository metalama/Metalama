[Introduction]
[Override]
internal class TargetClass
{
  [FieldOnly]
  [FieldAndProperty]
  private int _autoProperty;
  [FieldAndProperty]
  [PropertyOnly]
  public int AutoProperty
  {
    [MethodOnly]
    [return: ReturnValueOnly]
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._autoProperty;
    }
    [MethodOnly]
    [param: ParamOnly]
    [return: ReturnValueOnly]
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._autoProperty = value;
    }
  }
  [FieldAndProperty]
  [PropertyOnly]
  public int Property
  {
    [MethodOnly]
    [return: ReturnValueOnly]
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("Original Property");
      return 42;
    }
    [MethodOnly]
    [param: ParamOnly]
    [return: ReturnValueOnly]
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("Original Property");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.FieldOnlyAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.FieldAndPropertyAttribute]
  private global::System.Int32 _introducedAutoProperty;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.FieldAndPropertyAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.PropertyOnlyAttribute]
  public global::System.Int32 IntroducedAutoProperty
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.ReturnValueOnlyAttribute]
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._introducedAutoProperty;
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.ReturnValueOnlyAttribute]
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedAutoProperty = value;
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.FieldAndPropertyAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.PropertyOnlyAttribute]
  public global::System.Int32 IntroducedProperty
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.ReturnValueOnlyAttribute]
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("Original Property");
      return default(global::System.Int32);
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.ReturnValueOnlyAttribute]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes.ParamOnlyAttribute]
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("Original Property");
    }
  }
}