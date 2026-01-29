[LoggingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      global::System.Console.WriteLine("Getting");
      return (global::System.String? )_name;
    }
  }
  private readonly global::System.String? _name;
}
