[TestAspect]
internal class TargetClass
{
  public string Property
  {
    get
    {
      global::System.Console.WriteLine("Aspect code");
      Console.WriteLine("Aspect code");
      return "Test";
    }
    set
    {
      global::System.Console.WriteLine("Aspect code");
      Console.WriteLine("Aspect code");
    }
  }
  public string ExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("Aspect code");
      return "Test";
    }
  }
  private string? _autoProperty;
  public string? AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("Aspect code");
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("Aspect code");
      this._autoProperty = value;
    }
  }
  private string _autoPropertyWithInitializer = "Test";
  public string AutoPropertyWithInitializer
  {
    get
    {
      global::System.Console.WriteLine("Aspect code");
      return this._autoPropertyWithInitializer;
    }
    set
    {
      global::System.Console.WriteLine("Aspect code");
      this._autoPropertyWithInitializer = value;
    }
  }
}