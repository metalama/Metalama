[LoggingAspect]
internal class C
{
  private string? _name;
  public string? Name
  {
    get
    {
      global::System.Console.WriteLine("Getting");
      return this._name;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      this._name = value;
    }
  }
}
