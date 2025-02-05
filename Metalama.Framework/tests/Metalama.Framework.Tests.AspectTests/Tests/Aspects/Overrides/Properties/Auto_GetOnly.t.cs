internal class TargetClass : BaseClass
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
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly.TargetClass._staticProperty;
    }
    private set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly.TargetClass._staticProperty = value;
    }
  }
  private readonly int _initializerProperty = 42;
  [Override]
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
  private static int _staticInitializerProperty = 42;
  [Override]
  public static int StaticInitializerProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly.TargetClass._staticInitializerProperty;
    }
    private set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly.TargetClass._staticInitializerProperty = value;
    }
  }
  private readonly int _abstractBaseProperty;
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
  private readonly int _abstractBaseInitializerProperty = 42;
  [Override]
  public override int AbstractBaseInitializerProperty
  {
    get
    {
      return this.AbstractBaseInitializerProperty_Override;
    }
  }
  private global::System.Int32 AbstractBaseInitializerProperty_Override
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._abstractBaseInitializerProperty;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._abstractBaseInitializerProperty = value;
    }
  }
  private readonly int _virtualBaseProperty;
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
  private readonly int _virtualBaseInitializerProperty = 42;
  [Override]
  public override int VirtualBaseInitializerProperty
  {
    get
    {
      return this.VirtualBaseInitializerProperty_Override;
    }
  }
  private global::System.Int32 VirtualBaseInitializerProperty_Override
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._virtualBaseInitializerProperty;
    }
    init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._virtualBaseInitializerProperty = value;
    }
  }
  public TargetClass()
  {
    Property = 27;
    InitializerProperty = 27;
    AbstractBaseProperty_Override = 27;
    AbstractBaseInitializerProperty_Override = 27;
    VirtualBaseProperty_Override = 27;
    VirtualBaseInitializerProperty_Override = 27;
  }
  static TargetClass()
  {
    StaticProperty = 27;
    StaticInitializerProperty = 27;
  }
}