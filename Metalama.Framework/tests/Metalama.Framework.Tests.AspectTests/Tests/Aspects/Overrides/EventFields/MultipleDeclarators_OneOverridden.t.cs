[Test]
internal class TargetClass
{
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
  public event EventHandler? A, C;
}