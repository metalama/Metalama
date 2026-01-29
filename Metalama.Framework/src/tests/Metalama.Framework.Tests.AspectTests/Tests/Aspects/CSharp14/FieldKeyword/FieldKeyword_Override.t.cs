[LoggingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      return (global::System.String? )_name;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      _name = value;
    }
  }
  private global::System.String? _name;
}
