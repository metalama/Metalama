internal class Target
{
  private int _x;
  private int Foo()
  {
    Console.WriteLine("Before");
    this._x = this.Foo_Source();
    Console.WriteLine("After");
    return this._x;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}