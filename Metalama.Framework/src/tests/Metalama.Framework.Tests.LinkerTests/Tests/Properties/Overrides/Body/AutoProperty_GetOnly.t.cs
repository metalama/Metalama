class Target
{
  private readonly int _foo;
  int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return this._foo;
    }
    init
    {
      Console.WriteLine("Set");
      this._foo = value;
    }
  }
}