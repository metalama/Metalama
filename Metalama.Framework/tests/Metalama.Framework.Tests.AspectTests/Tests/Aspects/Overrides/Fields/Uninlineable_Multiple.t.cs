internal class TargetClass
{
  private global::System.Int32 Field_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._field;
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      this._field = value;
      this._field = value;
    }
  }
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.FirstOverrideAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.SecondOverrideAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.Field_SecondOverride;
      return this.Field_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      this.Field_SecondOverride = value;
      this.Field_SecondOverride = value;
    }
  }
  private global::System.Int32 StaticField_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._staticField;
      return this._staticField;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      this._staticField = value;
      this._staticField = value;
    }
  }
  private global::System.Int32 _staticField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.FirstOverrideAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.SecondOverrideAttribute]
  public global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.StaticField_SecondOverride;
      return this.StaticField_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      this.StaticField_SecondOverride = value;
      this.StaticField_SecondOverride = value;
    }
  }
  private global::System.Int32 InitializerField_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._initializerField;
      return this._initializerField;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      this._initializerField = value;
      this._initializerField = value;
    }
  }
  private global::System.Int32 _initializerField = 42;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.FirstOverrideAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.SecondOverrideAttribute]
  public global::System.Int32 InitializerField
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.InitializerField_SecondOverride;
      return this.InitializerField_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      this.InitializerField_SecondOverride = value;
      this.InitializerField_SecondOverride = value;
    }
  }
  private global::System.Int32 ReadOnlyField_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._readOnlyField;
      return this._readOnlyField;
    }
    init
    {
      global::System.Console.WriteLine("Second override.");
      this._readOnlyField = value;
      this._readOnlyField = value;
    }
  }
  private readonly global::System.Int32 _readOnlyField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.FirstOverrideAttribute]
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple.SecondOverrideAttribute]
  public global::System.Int32 ReadOnlyField
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.ReadOnlyField_SecondOverride;
      return this.ReadOnlyField_SecondOverride;
    }
    private init
    {
      global::System.Console.WriteLine("First override.");
      this.ReadOnlyField_SecondOverride = value;
      this.ReadOnlyField_SecondOverride = value;
    }
  }
  public TargetClass()
  {
    ReadOnlyField = 42;
  }
}