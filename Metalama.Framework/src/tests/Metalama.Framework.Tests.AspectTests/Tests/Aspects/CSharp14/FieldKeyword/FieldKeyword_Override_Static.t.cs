[LoggingAspect]
internal class C
{
  public static string? Name
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
  public static string? Description
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
  private static global::System.String? _description;
  private static global::System.String? _name;
}
