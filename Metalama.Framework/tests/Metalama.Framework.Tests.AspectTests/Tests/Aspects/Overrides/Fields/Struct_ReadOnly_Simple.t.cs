internal readonly struct TargetStruct
{
  private readonly global::System.Int32 _field;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TestAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._field;
    }
    private init
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._field = value;
    }
  }
  private static global::System.Int32 _staticField;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TestAttribute]
  public static global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TargetStruct._staticField;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TargetStruct._staticField = value;
    }
  }
  private static global::System.Int32 _staticReadOnlyField;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TestAttribute]
  public static global::System.Int32 StaticReadOnlyField
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TargetStruct._staticReadOnlyField;
    }
    private set
    {
      global::System.Console.WriteLine("This is aspect code.");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_ReadOnly_Simple.TargetStruct._staticReadOnlyField = value;
    }
  }
  public TargetStruct()
  {
    this._field = default;
  }
}