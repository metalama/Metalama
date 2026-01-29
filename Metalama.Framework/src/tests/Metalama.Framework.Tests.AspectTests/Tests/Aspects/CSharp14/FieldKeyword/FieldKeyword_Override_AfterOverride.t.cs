[ValidationAspect]
[TracingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      global::System.Console.WriteLine("Trace get Name");
      return (global::System.String? )_name;
    }
    set
    {
      global::System.Console.WriteLine("Trace set Name");
      if (string.IsNullOrEmpty(value))
      {
        throw new global::System.ArgumentException("Name cannot be null or empty");
      }
      _name = value;
    }
  }
  private global::System.String? _name;
}
