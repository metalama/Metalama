internal class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before");
      this._foo += value;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}