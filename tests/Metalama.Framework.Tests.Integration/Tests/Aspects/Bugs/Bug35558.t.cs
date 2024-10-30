[TestAspect]
public partial class TargetClass
{
  private global::System.Int32 _field11;
  private global::System.Int32 _field1
  {
    get
    {
      global::System.Console.WriteLine("Aspect");
      global::System.Console.WriteLine("Aspect");
      return this._field11;
    }
    set
    {
      global::System.Console.WriteLine("Aspect");
      global::System.Console.WriteLine("Aspect");
      this._field11 = value;
    }
  }
}