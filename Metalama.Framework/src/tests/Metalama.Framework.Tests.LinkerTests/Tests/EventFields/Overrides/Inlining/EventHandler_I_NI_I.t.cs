internal class Target
{
  private event EventHandler? _foo;
  private event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before2");
      this.Foo_Override1 += value;
      Console.WriteLine("After2");
    }
    remove
    {
      Console.WriteLine("Before2");
      this.Foo_Override1 -= value;
      Console.WriteLine("After2");
    }
  }
  private event EventHandler? Foo_Override1
  {
    add
    {
      Console.WriteLine("Before1");
      this._foo += value;
      Console.WriteLine("After1");
    }
    remove
    {
      Console.WriteLine("Before1");
      this._foo -= value;
      Console.WriteLine("After1");
    }
  }
}