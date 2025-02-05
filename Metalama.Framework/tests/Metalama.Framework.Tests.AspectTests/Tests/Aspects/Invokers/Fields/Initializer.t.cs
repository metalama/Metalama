[ResetInitializer]
[OverrideAndInitialize]
public class TestClass
{
  private global::System.Int32 _testField;
  public global::System.Int32 TestField
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._testField;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
      this._testField = value;
    }
  }
  private int _testProperty;
  public int TestProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._testProperty;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
      this._testProperty = value;
    }
  }
  public TestClass()
  {
    this.TestField = default;
    this.TestProperty = default;
  }
}