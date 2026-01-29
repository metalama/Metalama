[ValidationAspect]
[LoggingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      return (global::System.String? )_name1;
    }
    set
    {
      global::System.Console.WriteLine($"Setting Name to {value}");
      _name1 = value;
    }
  }
  private global::System.String? _name;
  private global::System.String? _name1;
}
