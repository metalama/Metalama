[IntroduceAndOverride]
internal class TargetClass
{
  private global::System.Int32 _field1;
  private global::System.Int32 _field
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._field1;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._field1 = value;
    }
  }
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return _field;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      _field = value;
    }
  }
  private static global::System.Int32 _staticField1;
  private static global::System.Int32 _staticField
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple.TargetClass._staticField1;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple.TargetClass._staticField1 = value;
    }
  }
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return _staticField;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      _staticField = value;
    }
  }
  public int ExpressionBodiedProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return 42;
    }
  }
  private int _autoProperty;
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._autoProperty = value;
    }
  }
  private readonly int _getOnlyAutoProperty;
  public int GetOnlyAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._getOnlyAutoProperty;
    }
    private init
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._getOnlyAutoProperty = value;
    }
  }
  private int _initializerAutoProperty = 42;
  public int InitializerAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._initializerAutoProperty;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._initializerAutoProperty = value;
    }
  }
  public TargetClass()
  {
    GetOnlyAutoProperty = 42;
  }
  private global::System.Int32 _introducedAutoProperty;
  public global::System.Int32 IntroducedAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._introducedAutoProperty;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._introducedAutoProperty = value;
    }
  }
  private readonly global::System.Int32 _introducedGetOnlyAutoProperty;
  public global::System.Int32 IntroducedGetOnlyAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return this._introducedGetOnlyAutoProperty;
    }
    private init
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      this._introducedGetOnlyAutoProperty = value;
    }
  }
  public global::System.Int32 IntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::System.Console.WriteLine("Second override.");
    }
  }
}