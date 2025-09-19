internal class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      this._foo -= value;
      Console.WriteLine("After");
    }
  }
}