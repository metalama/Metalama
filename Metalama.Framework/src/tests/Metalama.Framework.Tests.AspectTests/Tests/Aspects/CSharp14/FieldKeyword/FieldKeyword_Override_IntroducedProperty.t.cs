[IntroducePropertyAspect]
[LoggingAspect]
internal class C
{
  private global::System.String? _name;
  public global::System.String? Name
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
}
