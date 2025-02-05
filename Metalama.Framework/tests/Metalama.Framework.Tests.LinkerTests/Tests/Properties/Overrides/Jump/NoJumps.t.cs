class Target
{
  int _foo;
  int Foo
  {
    get
    {
      Console.WriteLine("Get2");
      Console.WriteLine("Get1");
      int foo;
      foo = _foo;
      if (foo > 0)
      {
        return foo;
      }
      else
      {
        return -foo;
      }
    }
    set
    {
      Console.WriteLine("Set2");
      Console.WriteLine("Set1");
      if (value != 0)
      {
        _foo = value;
      }
      else
      {
        throw new InvalidOperationException();
      }
    }
  }
}