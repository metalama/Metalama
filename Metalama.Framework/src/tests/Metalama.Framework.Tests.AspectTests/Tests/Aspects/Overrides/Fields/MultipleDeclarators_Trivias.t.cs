[Test]
internal class TargetClass
{
  // Comment before first list (no override).
  /// <summary>
  /// Doc comment for A, B, C.
  /// </summary>
  public // Comment after keyword.
 // Comment before variable declarator.
  int A, B, C // Comment after variable declarator
  ; // Comment after first list.
  // Comment before first list (one overridden).
  /// <summary>
  /// Doc comment for D, E, F.
  /// </summary>
  public // Comment after keyword.
 // Comment before variable declarator.
  int D, F // Comment after variable declarator
  ; // Comment after first list.
  private global::System.Int32 _e;
  public global::System.Int32 E
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._e;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._e = value;
    }
  }
  private global::System.Int32 _g;
  public global::System.Int32 G
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._g;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._g = value;
    }
  }
  private global::System.Int32 _h;
  public global::System.Int32 H
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._h;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._h = value;
    }
  }
  private global::System.Int32 _i;
  public global::System.Int32 I
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._i;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._i = value;
    }
  }
}