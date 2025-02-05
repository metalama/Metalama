internal class Foo
{
  private string? _property;
  [Normalize]
  public string? Property
  {
    get
    {
      return this._property;
    }
    set
    {
      this._property = value?.Trim().ToLowerInvariant();
    }
  }
}