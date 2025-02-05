[IntroduceField]
public class TestClass
{
  public int Property { get; }
  private readonly int _overriddenProperty;
  [OverrideProperty]
  public int OverriddenProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenProperty;
    }
    private init
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
  public global::System.Int32 IntroducedProperty { get; }
  private readonly global::System.Int32 _overriddenIntroducedProperty;
  public global::System.Int32 OverriddenIntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenIntroducedProperty;
    }
    private init
    {
      global::System.Console.WriteLine("Overridden");
      this._overriddenIntroducedProperty = value;
    }
  }
}