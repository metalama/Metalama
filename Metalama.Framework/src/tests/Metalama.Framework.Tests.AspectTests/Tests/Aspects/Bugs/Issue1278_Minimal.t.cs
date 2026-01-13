[Override]
private object? SomeProperty
{
  get
  {
    var activities = new global::System.String[]
    {
      "Activity1",
      "Activity2"
    };
    return this._someProperty;
  }
  set
  {
    this._someProperty = value;
  }
}