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
      global::System.Console.WriteLine($"Setting Name to {value}");
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
      global::System.Console.WriteLine($"Setting Description to {value}");
      _description = value;
    }
  }
  private static string GetDefault() => "Computed";
  private global::System.String? _description = GetDefault();
  private global::System.String? _name = "DefaultName";
}
