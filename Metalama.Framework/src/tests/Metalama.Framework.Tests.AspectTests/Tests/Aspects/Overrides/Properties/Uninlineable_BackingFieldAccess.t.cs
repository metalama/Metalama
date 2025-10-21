internal class TargetClass
{
  private string? _propertyWithGet;
  [Override]
  public string? PropertyWithGet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._propertyWithGet;
      return this._propertyWithGet;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._propertyWithGet = value;
      this._propertyWithGet = value;
    }
  }
  private string? PropertyWithGet_Source { get => field; set; }
  private string? _propertyWithSet;
  [Override]
  public string? PropertyWithSet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._propertyWithSet;
      return this._propertyWithSet;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._propertyWithSet = value;
      this._propertyWithSet = value;
    }
  }
  private string? PropertyWithSet_Source { get; set => field = value; }
  private string? _propertyWithGetSet;
  [Override]
  public string? PropertyWithGetSet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._propertyWithGetSet;
      return this._propertyWithGetSet;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._propertyWithGetSet = value;
      this._propertyWithGetSet = value;
    }
  }
  private string? PropertyWithGetSet_Source { get => field; set => field = value; }
}