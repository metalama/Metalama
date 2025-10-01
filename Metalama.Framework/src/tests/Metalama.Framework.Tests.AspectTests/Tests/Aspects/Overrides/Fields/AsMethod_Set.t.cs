internal class TargetClass
{
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Set.OverrideAttribute]
  private global::System.Int32 _field = 42;
  public global::System.Int32 Field
  {
    get
    {
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("Overridden setter");
      this._field = value;
    }
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Set.OverrideAttribute]
  private static global::System.Int32 _staticField = 24;
  public static global::System.Int32 StaticField
  {
    get
    {
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Set.TargetClass._staticField;
    }
    set
    {
      global::System.Console.WriteLine("Overridden setter");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Set.TargetClass._staticField = value;
    }
  }
}