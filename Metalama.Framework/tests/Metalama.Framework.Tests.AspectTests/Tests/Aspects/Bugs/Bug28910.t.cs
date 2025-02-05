internal class EmptyOverrideFieldOrPropertyExample
{
  private global::System.Int32 _field1;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28910.EmptyOverrideFieldOrPropertyAttribute]
  private global::System.Int32 _field
  {
    get
    {
      return this._field1;
    }
    set
    {
      this._field1 = value;
    }
  }
}