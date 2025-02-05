internal class TargetClass
{
  private global::System.Int32 _implicitlyPrivateField1;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  private global::System.Int32 _implicitlyPrivateField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._implicitlyPrivateField1;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._implicitlyPrivateField1 = value;
    }
  }
  private global::System.Int32 _privateField1;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  private global::System.Int32 _privateField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._privateField1;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._privateField1 = value;
    }
  }
  private global::System.Int32 _privateProtectedField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  private protected global::System.Int32 PrivateProtectedField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._privateProtectedField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._privateProtectedField = value;
    }
  }
  private global::System.Int32 _protectedField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  protected global::System.Int32 ProtectedField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._protectedField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._protectedField = value;
    }
  }
  private global::System.Int32 _protectedInternalField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  protected internal global::System.Int32 ProtectedInternalField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._protectedInternalField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._protectedInternalField = value;
    }
  }
  private global::System.Int32 _internalField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  internal global::System.Int32 InternalField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._internalField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._internalField = value;
    }
  }
  private global::System.Int32 _publicField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility.OverrideAttribute]
  public global::System.Int32 PublicField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._publicField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._publicField = value;
    }
  }
}