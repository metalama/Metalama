[LogAccessAspect]
internal class C
{
  private global::System.Int32 _field1;
  private global::System.Int32 _field
  {
    get
    {
      global::System.Console.WriteLine("Getting _field");
      return this._field1;
    }
    set
    {
      global::System.Console.WriteLine("Setting _field");
      this._field1 = value;
    }
  }
}
