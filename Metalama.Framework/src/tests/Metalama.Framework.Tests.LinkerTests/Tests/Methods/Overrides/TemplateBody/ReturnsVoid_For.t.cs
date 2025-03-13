class Target
{
  void Foo()
  {
    Console.WriteLine("Before");
    for (int i = 0; i < 5; i++)
    {
      Console.WriteLine("Original");
    }
    Console.WriteLine("After");
  }
}