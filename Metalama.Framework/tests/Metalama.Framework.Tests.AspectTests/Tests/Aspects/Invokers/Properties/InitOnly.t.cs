[IntroduceField]
public class TestClass
{
  public int Property { get; init; }
  private readonly int _overriddenProperty;
  [OverrideProperty]
  public int OverriddenProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenProperty;
    }
    init
    {
      global::System.Console.WriteLine("Overridden");
      this._overriddenProperty = value;
    }
  }
  [InvokeBefore]
  [InvokeAfter]
  public TestClass()
  {
    // --- Before ---
    // Base
    this.IntroducedProperty = 42;
    this._overriddenIntroducedProperty = 42;
    this._overriddenProperty = 42;
    this.Property = 42;
    // Current
    this.IntroducedProperty = 42;
    this._overriddenIntroducedProperty = 42;
    this._overriddenProperty = 42;
    this.Property = 42;
    // Final
    this.IntroducedProperty = 42;
    this.OverriddenIntroducedProperty = 42;
    this.OverriddenProperty = 42;
    this.Property = 42;
    // --- After ---
    // Base
    this.IntroducedProperty = 42;
    this.OverriddenIntroducedProperty = 42;
    this.OverriddenProperty = 42;
    this.Property = 42;
    // Current
    this.IntroducedProperty = 42;
    this.OverriddenIntroducedProperty = 42;
    this.OverriddenProperty = 42;
    this.Property = 42;
    // Final
    this.IntroducedProperty = 42;
    this.OverriddenIntroducedProperty = 42;
    this.OverriddenProperty = 42;
    this.Property = 42;
  }
  public global::System.Int32 IntroducedProperty { get; init; }
  private readonly global::System.Int32 _overriddenIntroducedProperty;
  public global::System.Int32 OverriddenIntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenIntroducedProperty;
    }
    init
    {
      global::System.Console.WriteLine("Overridden");
      this._overriddenIntroducedProperty = value;
    }
  }
}
public class UsingClass
{
  public void Foo()
  {
    new TestClass()
    {
      Property = 42,
      OverriddenProperty = 42,
      IntroducedProperty = 42,
      OverriddenIntroducedProperty = 42,
    };
  }
}