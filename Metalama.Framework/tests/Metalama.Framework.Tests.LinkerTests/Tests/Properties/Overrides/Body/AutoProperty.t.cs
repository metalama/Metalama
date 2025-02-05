class Target
{
  private int _foo;
  int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return this._foo;
    }
    set
    {
      Console.WriteLine("Set");
      this._foo = value;
    }
  }
}