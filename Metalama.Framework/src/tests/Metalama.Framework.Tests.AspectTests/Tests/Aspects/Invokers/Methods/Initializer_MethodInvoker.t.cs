[Override]
[Initializer]
public class TestClass
{
  private int _property1;
  public int Property1
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._property1;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
      this._property1 = value;
    }
  }
  private int _property2;
  public int Property2
  {
    get
    {
      global::System.Console.WriteLine("Overridden");
      return this._property2;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
      this._property2 = value;
    }
  }
  public TestClass()
  {
    this._property1 = 42;
    this._property2 = 42;
    this._property1 = 42;
    this._property2 = 42;
    this.Property1 = 42;
    this.Property2 = 42;
  }
}
