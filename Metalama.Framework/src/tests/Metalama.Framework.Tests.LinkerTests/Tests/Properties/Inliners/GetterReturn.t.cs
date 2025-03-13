class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      return 42;
    }
  }
}