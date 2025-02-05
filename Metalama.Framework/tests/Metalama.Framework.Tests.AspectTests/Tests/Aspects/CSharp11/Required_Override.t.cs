public class C
{
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.Required_Override.TheAspect]
  public required global::System.Int32 Field
  {
    get
    {
      return this._field;
    }
    set
    {
      this._field = value;
    }
  }
  private int _property;
  [TheAspect]
  public required int Property
  {
    get
    {
      return this._property;
    }
    set
    {
      this._property = value;
    }
  }
}