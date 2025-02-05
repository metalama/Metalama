internal record class Target([NotNull] string X)
{
  public string Y { get; set; } = X;
  private readonly string _x = X;
  public string X
  {
    get
    {
      return this._x;
    }
    init
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("X");
      }
      this._x = value;
    }
  }
}