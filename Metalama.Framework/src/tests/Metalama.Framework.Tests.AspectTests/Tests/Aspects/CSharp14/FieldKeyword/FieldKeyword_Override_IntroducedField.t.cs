[IntroduceFieldAspect]
[LoggingAspect]
internal class C
{
  private global::System.String? _name1;
  public global::System.String? _name
  {
    get
    {
      return (global::System.String? )_name1;
    }
    set
    {
      global::System.Console.WriteLine($"Setting _name to {value}");
      _name1 = value;
    }
  }
}
