internal readonly struct TargetStruct
{
  private readonly int _property;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._property;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._property = value;
    }
  }
  private static int _staticProperty;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Struct_ReadOnly_Auto.TargetStruct._staticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Struct_ReadOnly_Auto.TargetStruct._staticProperty = value;
    }
  }
  private readonly int _propertyInitOnly;
  [Override]
  public int PropertyInitOnly
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._propertyInitOnly;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._propertyInitOnly = value;
    }
  }
  private readonly int _staticPropertyInitOnly;
  [Override]
  public int StaticPropertyInitOnly
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._staticPropertyInitOnly;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._staticPropertyInitOnly = value;
    }
  }
  public TargetStruct()
  {
    this._property = default;
    this._propertyInitOnly = default;
    this._staticPropertyInitOnly = default;
  }
}