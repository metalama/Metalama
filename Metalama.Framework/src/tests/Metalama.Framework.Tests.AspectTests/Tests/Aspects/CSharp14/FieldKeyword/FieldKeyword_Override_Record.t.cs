[LoggingAspect]
internal record C(string? Name, string? Title)
{
  public string? Description
  {
    get
    {
      return (global::System.String? )_description;
    }
    init
    {
      global::System.Console.WriteLine($"Init Description to {value}");
      _description = value;
    }
  }
  private global::System.String? _description;
  private global::System.String? _name;
  private global::System.String? _title;
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
}
