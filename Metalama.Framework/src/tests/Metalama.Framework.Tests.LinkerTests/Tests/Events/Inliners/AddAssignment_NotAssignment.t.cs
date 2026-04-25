public class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before");
      this._foo?.Invoke(null, EventArgs.Empty);
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}