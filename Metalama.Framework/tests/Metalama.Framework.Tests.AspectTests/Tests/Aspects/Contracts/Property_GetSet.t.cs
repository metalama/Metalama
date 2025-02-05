internal class Target
{
  private string? q;
  private string _p = default !;
  [NotNull]
  public string P
  {
    get
    {
      return this._p;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._p = value;
    }
  }
  [NotNull]
  public string Q
  {
    get => q!;
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      q = value + "-";
    }
  }
}