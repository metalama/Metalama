internal class TargetClass
{
  private int _property_ImplicitSet;
  [Override]
  public int Property_ImplicitSet
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _property_ImplicitSet;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _property_ImplicitSet = value;
    }
  }
  private readonly int _property_ImplicitInit;
  [Override]
  public int Property_ImplicitInit
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _property_ImplicitInit;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _property_ImplicitInit = value;
    }
  }
  private int _property_ImplicitGet;
  [Override]
  public int Property_ImplicitGet
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return_property_ImplicitGet;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("This is the original setter.");
      _property_ImplicitGet = value;
    }
  }
  private readonly int _property_GetOnly;
  [Override]
  public int Property_GetOnly
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _property_GetOnly;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _property_GetOnly = value;
    }
  }
  private static int _staticProperty_Get;
  [Override]
  public static int StaticProperty_Get
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _staticProperty_Get;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _staticProperty_Get = value;
    }
  }
  private static int _staticProperty_Set;
  [Override]
  public static int StaticProperty_Set
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return_staticProperty_Set;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("This is the original setter.");
      _staticProperty_Set = value;
    }
  }
}