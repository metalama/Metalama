[LoggingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      return (global::System.String? )_name;
    }
    init
    {
      global::System.Console.WriteLine($"Init Name to {value}");
      _name = value;
    }
  }
  public string? Title
  {
    get
    {
      return (global::System.String? )_title;
    }
    init
    {
      global::System.Console.WriteLine($"Init Title to {value}");
      _title = value;
    }
  }
  private global::System.String? _name;
  private global::System.String? _title = "Default";
}
