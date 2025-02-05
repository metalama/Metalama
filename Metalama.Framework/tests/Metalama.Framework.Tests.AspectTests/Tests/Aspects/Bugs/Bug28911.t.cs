internal class EmptyOverrideFieldOrPropertyExample
{
  private string? _property;
  [EmptyOverrideFieldOrProperty]
  public string? Property
  {
    get
    {
      return this._property;
    }
    set
    {
      this._property = value;
    }
  }
}