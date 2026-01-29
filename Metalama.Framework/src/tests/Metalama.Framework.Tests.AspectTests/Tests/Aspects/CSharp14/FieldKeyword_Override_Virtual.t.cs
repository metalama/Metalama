[LoggingAspect]
internal class C : BaseClass
{
  public virtual string? Name
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
  public override string? BaseName
  {
    get
    {
      return (global::System.String? )_baseName;
    }
    set
    {
      global::System.Console.WriteLine($"Setting BaseName to {value}");
      _baseName = value;
    }
  }
  private global::System.String? _baseName;
  private global::System.String? _name;
}
