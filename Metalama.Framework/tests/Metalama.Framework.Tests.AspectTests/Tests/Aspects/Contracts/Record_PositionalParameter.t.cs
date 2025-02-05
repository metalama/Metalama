internal record Target([NotNull] string m)
{
  private readonly string _m1 = m;
  public string m
  {
    get
    {
      return this._m1;
    }
    init
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("m");
      }
      this._m1 = value;
    }
  }
}