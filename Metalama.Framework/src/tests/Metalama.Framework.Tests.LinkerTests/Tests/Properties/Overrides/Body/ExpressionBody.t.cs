internal class Target
{
  private int _foo = 0;
  private int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return this._foo;
    }
  }
}