internal class Target
{
  // This is just to keep out parameter possible.
  private int _z;
  public Target([NotNull] out int x)
  {
    this._z = x = 42;
    if (x == null)
    {
      throw new global::System.ArgumentNullException("x");
    }
  }
}
