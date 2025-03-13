[IntroduceAndOverride]
internal class TargetClass
{
  private global::System.Int32 _field1;
  private global::System.Int32 _field
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._field1;
      return this._field1;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this._field1 = value;
    }
  }
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this.Property_Source;
      return this.Property_Source;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this.Property_Source = value;
    }
  }
  private int Property_Source
  {
    get
    {
      return _field;
    }
    set
    {
      _field = value;
    }
  }
  private static global::System.Int32 _staticField1;
  private static global::System.Int32 _staticField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass._staticField1;
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass._staticField1;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass._staticField1 = value;
    }
  }
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass.StaticProperty_Source;
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass.StaticProperty_Source;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple_MethodTemplates.TargetClass.StaticProperty_Source = value;
    }
  }
  private static int StaticProperty_Source
  {
    get
    {
      return _staticField;
    }
    set
    {
      _staticField = value;
    }
  }
  public int ExpressionBodiedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this.ExpressionBodiedProperty_Source;
      return this.ExpressionBodiedProperty_Source;
    }
  }
  private int ExpressionBodiedProperty_Source => 42;
  private int _autoProperty;
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._autoProperty;
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this._autoProperty = value;
    }
  }
  private readonly int _getOnlyAutoProperty;
  public int GetOnlyAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._getOnlyAutoProperty;
      return this._getOnlyAutoProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this._getOnlyAutoProperty = value;
    }
  }
  private int _initializerAutoProperty = 42;
  public int InitializerAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._initializerAutoProperty;
      return this._initializerAutoProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
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
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._introducedAutoProperty;
      return this._introducedAutoProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedAutoProperty = value;
    }
  }
  private readonly global::System.Int32 _introducedGetOnlyAutoProperty;
  public global::System.Int32 IntroducedGetOnlyAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this._introducedGetOnlyAutoProperty;
      return this._introducedGetOnlyAutoProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedGetOnlyAutoProperty = value;
    }
  }
  private global::System.Int32 IntroducedProperty_IntroduceAndOverride
  {
    get
    {
      return (global::System.Int32)42;
    }
    set
    {
    }
  }
  public global::System.Int32 IntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      global::System.Console.WriteLine("This is the overridden getter.");
      _ = this.IntroducedProperty_IntroduceAndOverride;
      return this.IntroducedProperty_IntroduceAndOverride;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::System.Console.WriteLine("This is the overridden setter.");
      this.IntroducedProperty_IntroduceAndOverride = value;
    }
  }
}
