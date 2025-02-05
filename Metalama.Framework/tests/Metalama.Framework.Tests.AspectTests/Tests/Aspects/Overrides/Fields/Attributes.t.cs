[Introduction]
[Override]
internal class TargetClass
{
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes.FieldOnlyAttribute]
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes.FieldAndPropertyAttribute]
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
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes.FieldOnlyAttribute]
  private global::System.Int32 _introducedField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes.FieldAndPropertyAttribute]
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
}