public class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before");
      EventHandler? x = null;
      x += this._foo;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}