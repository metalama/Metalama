class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return 42;
    }
    set
    {
      Console.WriteLine("Set");
    }
  }
}