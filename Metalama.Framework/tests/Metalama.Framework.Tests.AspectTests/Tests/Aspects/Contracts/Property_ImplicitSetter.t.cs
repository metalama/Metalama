internal class Target
{
  private readonly string _q = default !;
  [NotNull]
  public string Q
  {
    get
    {
      return this._q;
    }
    private init
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._q = value;
    }
  }
}