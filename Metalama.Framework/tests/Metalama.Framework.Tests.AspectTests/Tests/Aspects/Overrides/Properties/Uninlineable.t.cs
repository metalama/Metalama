internal class TargetClass
{
  private int _field;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this.Property_Source;
      return this.Property_Source;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this.Property_Source = value;
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
  private static int _staticField;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable.TargetClass.StaticProperty_Source;
      return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable.TargetClass.StaticProperty_Source;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable.TargetClass.StaticProperty_Source = value;
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable.TargetClass.StaticProperty_Source = value;
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
  [Override]
  public int ExpressionBodiedProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this.ExpressionBodiedProperty_Source;
      return this.ExpressionBodiedProperty_Source;
    }
  }
  private int ExpressionBodiedProperty_Source => 42;
  private int _autoProperty;
  [Override]
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._autoProperty;
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this._autoProperty = value;
      this._autoProperty = value;
    }
  }
  private readonly int _autoGetOnlyProperty;
  [Override]
  public int AutoGetOnlyProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this._autoGetOnlyProperty;
      return this._autoGetOnlyProperty;
    }
    private init
    {
      global::System.Console.WriteLine("Override.");
      this._autoGetOnlyProperty = value;
      this._autoGetOnlyProperty = value;
    }
  }
}