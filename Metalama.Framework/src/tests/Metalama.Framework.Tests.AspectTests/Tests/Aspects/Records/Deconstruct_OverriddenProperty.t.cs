[Override]
internal record Target(int X, string Y)
{
  public void Deconstruct(out global::System.Int32 X, out global::System.String Y)
  {
    // <target>
    X = default;
    Y = default!;
    X = this.X;
    Y = this.Y;
  }
  private readonly int _x = X;
  public int X
  {
    get
    {
      return this._x;
    }
    init
    {
      this._x = value;
    }
  }
}