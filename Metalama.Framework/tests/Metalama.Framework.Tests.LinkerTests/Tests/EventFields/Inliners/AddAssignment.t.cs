class Target
{
  private event EventHandler? _foo;
  event EventHandler? Foo
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