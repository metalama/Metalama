internal class TargetClass
{
  private int _field;
  [FirstOverride]
  [SecondOverride]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.Property_SecondOverride;
      return this.Property_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      this.Property_SecondOverride = value;
      this.Property_SecondOverride = value;
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
  private global::System.Int32 Property_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this.Property_Source;
      return this.Property_Source;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      this.Property_Source = value;
      this.Property_Source = value;
    }
  }
  private static int _staticField;
  [FirstOverride]
  [SecondOverride]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_SecondOverride;
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_SecondOverride = value;
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_SecondOverride = value;
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
  private static global::System.Int32 StaticProperty_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_Source;
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_Source;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_Source = value;
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_Multiple.TargetClass.StaticProperty_Source = value;
    }
  }
  [FirstOverride]
  [SecondOverride]
  public int ExpressionBodiedProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.ExpressionBodiedProperty_SecondOverride;
      return this.ExpressionBodiedProperty_SecondOverride;
    }
  }
  private int ExpressionBodiedProperty_Source => 42;
  private global::System.Int32 ExpressionBodiedProperty_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this.ExpressionBodiedProperty_Source;
      return this.ExpressionBodiedProperty_Source;
    }
  }
  private int _autoProperty;
  [FirstOverride]
  [SecondOverride]
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.AutoProperty_SecondOverride;
      return this.AutoProperty_SecondOverride;
    }
    set
    {
      global::System.Console.WriteLine("First override.");
      this.AutoProperty_SecondOverride = value;
      this.AutoProperty_SecondOverride = value;
    }
  }
  private global::System.Int32 AutoProperty_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._autoProperty;
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("Second override.");
      this._autoProperty = value;
      this._autoProperty = value;
    }
  }
  private readonly int _autoGetOnlyProperty;
  [FirstOverride]
  [SecondOverride]
  public int AutoGetOnlyProperty
  {
    get
    {
      global::System.Console.WriteLine("First override.");
      _ = this.AutoGetOnlyProperty_SecondOverride;
      return this.AutoGetOnlyProperty_SecondOverride;
    }
    private init
    {
      global::System.Console.WriteLine("First override.");
      this.AutoGetOnlyProperty_SecondOverride = value;
      this.AutoGetOnlyProperty_SecondOverride = value;
    }
  }
  private global::System.Int32 AutoGetOnlyProperty_SecondOverride
  {
    get
    {
      global::System.Console.WriteLine("Second override.");
      _ = this._autoGetOnlyProperty;
      return this._autoGetOnlyProperty;
    }
    init
    {
      global::System.Console.WriteLine("Second override.");
      this._autoGetOnlyProperty = value;
      this._autoGetOnlyProperty = value;
    }
  }
  public TargetClass()
  {
    AutoGetOnlyProperty = 42;
  }
}