internal class TargetClass : BaseClass
{
  private readonly int _property;
  // Comment before property.
  /// <summary>
  /// Gets the property value.
  /// </summary>
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
  /// <summary>
  /// Gets the static property value.
  /// </summary>
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly_Trivias.TargetClass._staticProperty;
    }
    private set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly_Trivias.TargetClass._staticProperty = value;
    }
  }
  private readonly int _initializerProperty = 42;
  /// <summary>
  /// Gets the initializer property value.
  /// </summary>
  [Override]
  [Description("An initializer property")]
  public int InitializerProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._initializerProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._initializerProperty = value;
    }
  }
  private readonly int _abstractBaseProperty;
  /// <summary>
  /// Gets the abstract base property value.
  /// </summary>
  [Override]
  public override int AbstractBaseProperty
  {
    get
    {
      return this.AbstractBaseProperty_Override;
    }
  }
  private global::System.Int32 AbstractBaseProperty_Override
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._abstractBaseProperty;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._abstractBaseProperty = value;
    }
  }
  private readonly int _virtualBaseProperty;
  /// <summary>
  /// Gets the virtual base property value.
  /// </summary>
  [Override]
  public override int VirtualBaseProperty
  {
    get
    {
      return this.VirtualBaseProperty_Override;
    }
  }
  private global::System.Int32 VirtualBaseProperty_Override
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._virtualBaseProperty;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._virtualBaseProperty = value;
    }
  }
  public TargetClass()
  {
    Property = 27;
    InitializerProperty = 27;
    AbstractBaseProperty_Override = 27;
    VirtualBaseProperty_Override = 27;
  }
  static TargetClass()
  {
    StaticProperty = 27;
  }
}
