// Final Compilation.Emit failed.
// Error CS0182 on `int.Parse("42")`: `An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type`
// Error CS0617 on `PropertyValue`: `'PropertyValue' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.`
internal class TargetCode
{
  [Aspect(int.Parse("42"))]
  public int Foo { get; set; }
  private int _bar;
  [Aspect(PropertyValue = int.Parse("42"))]
  public int Bar
  {
    get
    {
      global::System.Console.WriteLine("ConstructorValue: 0");
      global::System.Console.WriteLine("PropertyValue: 0");
      return this._bar;
    }
    set
    {
      this._bar = value;
    }
  }
}