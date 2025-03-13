[Test]
internal class TargetClass
{
  private global::System.Int32 _a;
  public global::System.Int32 A
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._a;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._a = value;
    }
  }
  private global::System.Int32 _b;
  public global::System.Int32 B
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._b;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._b = value;
    }
  }
  private global::System.Int32 _c;
  public global::System.Int32 C
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._c;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._c = value;
    }
  }
// Comment after.
}