internal class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
      this.Foo_Override2 += value;
    }
    remove
    {
      this.Foo_Override2 -= value;
    }
  }
  private event EventHandler? Foo_Override2
  {
    add
    {
      Console.WriteLine("Before2");
      Console.WriteLine("Before1");
      this._foo += value;
      Console.WriteLine("After1");
      Console.WriteLine("After2");
    }
    remove
    {
      Console.WriteLine("Before2");
      Console.WriteLine("Before1");
      this._foo -= value;
      Console.WriteLine("After1");
      Console.WriteLine("After2");
    }
  }
}