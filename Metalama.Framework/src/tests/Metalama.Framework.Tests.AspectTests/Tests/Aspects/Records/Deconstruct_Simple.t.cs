[Override]
internal record Target(int X, string Y)
{
  public void Deconstruct(out global::System.Int32 X, out global::System.String Y)
  {
    // <target>
    X = default;
    Y = default !;
    X = this.X;
    Y = this.Y;
  }
}