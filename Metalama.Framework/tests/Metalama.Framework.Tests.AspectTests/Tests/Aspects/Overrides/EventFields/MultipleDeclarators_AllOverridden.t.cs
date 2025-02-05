[Test]
internal class TargetClass
{
  private event EventHandler? _a;
  public event EventHandler? A
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._a += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._a -= value;
    }
  }
  private event EventHandler? _b;
  public event EventHandler? B
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._b += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._b -= value;
    }
  }
  private event EventHandler? _c;
  public event EventHandler? C
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._c += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._c -= value;
    }
  }
}