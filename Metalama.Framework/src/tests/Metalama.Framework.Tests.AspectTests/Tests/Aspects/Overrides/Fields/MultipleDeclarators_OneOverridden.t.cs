[Test]
internal class TargetClass
{
  // Comment before.
  public int A, C;
  private global::System.Int32 _b;
  // Comment before.
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
// Comment after.
}