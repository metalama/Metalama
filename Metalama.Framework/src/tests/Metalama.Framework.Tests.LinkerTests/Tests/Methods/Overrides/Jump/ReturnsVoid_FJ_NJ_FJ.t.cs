class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Before3");
    Console.WriteLine("Before2");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    Console.WriteLine("Before1");
    Console.WriteLine("Original Start");
    if (x == 0)
    {
      goto __aspect_return_2;
    }
    Console.WriteLine("Original End");
    __aspect_return_2:
      Console.WriteLine("After1");
    Console.WriteLine("After2");
    __aspect_return_1:
      Console.WriteLine("After3");
  }
}