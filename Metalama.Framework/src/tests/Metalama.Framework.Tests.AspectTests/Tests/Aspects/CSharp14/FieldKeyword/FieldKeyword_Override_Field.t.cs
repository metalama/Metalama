internal class TargetClass
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Field.LoggingAspect]
  public global::System.String? Name
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
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Field.LoggingAspect]
  public global::System.Int32 Count
  {
    get
    {
      return (global::System.Int32)_count;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      _count = value;
    }
  }
  private global::System.Int32 _count;
  private global::System.String? _name;
}
