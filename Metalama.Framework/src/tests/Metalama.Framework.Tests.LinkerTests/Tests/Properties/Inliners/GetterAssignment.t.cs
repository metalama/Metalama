class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      int x;
      Console.WriteLine("Original");
      x = 42;
      Console.WriteLine("After");
      return x;
    }
  }
}