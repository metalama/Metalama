internal record struct Target([NotNull] string X)
{
  public string Y { get; set; } = X;
  private string _x = X;
  public string X
  {
    get
    {
      return this._x;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("X");
      }
      this._x = value;
    }
  }
}