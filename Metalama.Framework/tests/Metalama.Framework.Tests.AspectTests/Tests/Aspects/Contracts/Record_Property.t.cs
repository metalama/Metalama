 internal record Target
{
  private string _m = default !;
  [NotNull]
  public string M
  {
    get
    {
      return this._m;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("M");
      }
      this._m = value;
    }
  }
}