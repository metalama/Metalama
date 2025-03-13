internal class TargetClass
{
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable.OverrideAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._field;
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._field = value;
      this._field = value;
    }
  }
  private global::System.Int32 _staticField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable.OverrideAttribute]
  public global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._staticField;
      return this._staticField;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._staticField = value;
      this._staticField = value;
    }
  }
  private global::System.Int32 _initializerField = 42;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable.OverrideAttribute]
  public global::System.Int32 InitializerField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._initializerField;
      return this._initializerField;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._initializerField = value;
      this._initializerField = value;
    }
  }
  private readonly global::System.Int32 _readOnlyField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable.OverrideAttribute]
  public global::System.Int32 ReadOnlyField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._readOnlyField;
      return this._readOnlyField;
    }
    private init
    {
      global::System.Console.WriteLine("Override.");
      this._readOnlyField = value;
      this._readOnlyField = value;
    }
  }
  public TargetClass()
  {
    ReadOnlyField = 42;
  }
}