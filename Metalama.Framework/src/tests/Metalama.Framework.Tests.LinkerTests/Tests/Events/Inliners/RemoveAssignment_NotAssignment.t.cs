public class Target
{
  private event EventHandler? _foo;
  event EventHandler? Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      this._foo?.Invoke(null, new EventArgs());
      Console.WriteLine("After");
    }
  }
}