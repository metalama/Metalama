class Target
{
  int _foo = 0;
  int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return this._foo;
    }
  }
}