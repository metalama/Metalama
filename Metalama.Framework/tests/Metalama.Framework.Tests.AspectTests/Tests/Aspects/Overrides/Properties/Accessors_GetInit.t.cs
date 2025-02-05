internal class TargetClass
{
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  private int PrivateProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  public int RestrictedGetProperty
  {
    private get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  protected int ProtectedRestrictedGetProperty
  {
    private get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  public int RestrictedSetProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  protected int ProtectedestrictedSetProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  public int GetExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
  [Override]
  public int InitExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine($"This is the original setter, setting {value}.");
    }
  }
}