[Introduction]
[Override]
internal class TargetClass
{
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable.FieldOnlyAttribute]
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable.FieldAndPropertyAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._field;
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._field = value;
      this._field = value;
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable.FieldOnlyAttribute]
  private global::System.Int32 _introducedField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable.FieldAndPropertyAttribute]
  public global::System.Int32 IntroducedField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._introducedField;
      return this._introducedField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedField = value;
      this._introducedField = value;
    }
  }
}