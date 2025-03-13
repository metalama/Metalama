class Target
{
  private event EventHandler? _foo;
  event EventHandler? Foo
  {
    add
    {
      this.Foo_Override += value;
    }
    remove
    {
      this.Foo_Override -= value;
    }
  }
  event EventHandler? Foo_Override
  {
    add
    {
      Console.WriteLine("Before");
      this._foo += value;
      Console.WriteLine("After");
    }
    remove
    {
      Console.WriteLine("Before");
      this._foo -= value;
      Console.WriteLine("After");
    }
  }
}