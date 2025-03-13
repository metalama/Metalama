[IntroduceField]
public class TestClass
{
  private readonly int Field;
  private readonly global::System.Int32 _overriddenField;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.ReadOnly.OverrideFieldAttribute]
  private global::System.Int32 OverriddenField
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenField;
    }
    init
    {
      global::System.Console.WriteLine("Overridden");
      this._overriddenField = value;
    }
  }
  [InvokeBefore]
  [InvokeAfter]
  public TestClass()
  { // --- Before ---
    // Base
    this.Field = 42;
    this.IntroducedField = 42;
    this._overriddenField = 42;
    this._overriddenIntroducedField = 42;
    // Current
    this.Field = 42;
    this.IntroducedField = 42;
    this._overriddenField = 42;
    this._overriddenIntroducedField = 42;
    // Final
    this.Field = 42;
    this.IntroducedField = 42;
    this.OverriddenField = 42;
    this.OverriddenIntroducedField = 42;
    // --- After ---
    // Base
    this.Field = 42;
    this.IntroducedField = 42;
    this.OverriddenField = 42;
    this.OverriddenIntroducedField = 42;
    // Current
    this.Field = 42;
    this.IntroducedField = 42;
    this.OverriddenField = 42;
    this.OverriddenIntroducedField = 42;
    // Final
    this.Field = 42;
    this.IntroducedField = 42;
    this.OverriddenField = 42;
    this.OverriddenIntroducedField = 42;
  }
  private readonly global::System.Int32 IntroducedField;
  private readonly global::System.Int32 _overriddenIntroducedField;
  private global::System.Int32 OverriddenIntroducedField
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._overriddenIntroducedField;
    }
    init
    {
      global::System.Console.WriteLine("Overridden");
      this._overriddenIntroducedField = value;
    }
  }
}