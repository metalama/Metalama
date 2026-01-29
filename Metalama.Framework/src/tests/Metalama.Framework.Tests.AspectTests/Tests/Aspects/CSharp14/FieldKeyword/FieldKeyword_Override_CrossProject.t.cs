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
  public string? Description
  {
    get
    {
      return (global::System.String? )_description;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      _description = value;
    }
  }
  private global::System.String? _description;
  private global::System.String? _name;
}
