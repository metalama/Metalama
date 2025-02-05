public class Target
{
  private event EventHandler? _foo;
  event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before");
      this._foo?.Invoke(null, new EventArgs());
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}