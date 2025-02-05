internal class Target
{
  private string? q;
  private string _p = default !;
  public string P
  {
    get
    {
      return this._p;
    }
    [param: NotNull]
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._p = value;
    }
  }
  public string Q
  {
    get => q!;
    [param: NotNull]
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