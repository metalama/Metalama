internal class TargetClass
{
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get.OverrideAttribute]
  private global::System.Int32 _field = 42;
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      return this._field;
    }
    set
    {
      this._field = value;
    }
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get.OverrideAttribute]
  private static global::System.Int32 _staticField = 24;
  public static global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get.TargetClass._staticField;
    }
    set
    {
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get.TargetClass._staticField = value;
    }
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get.OverrideAttribute]
  private readonly global::System.Int32 _readOnlyField = 12;
  public global::System.Int32 ReadOnlyField
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      return this._readOnlyField;
    }
    private init
    {
      this._readOnlyField = value;
    }
  }
}