class Target
{
  void Foo()
  {
    Console.WriteLine("Before");
    int i = 0;
    while (i < 5)
    {
      Console.WriteLine("Original");
      i++;
    }
    Console.WriteLine("After");
  }
}