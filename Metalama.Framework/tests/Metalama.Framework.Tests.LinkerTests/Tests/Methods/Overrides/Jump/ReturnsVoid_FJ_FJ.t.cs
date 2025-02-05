class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Before2");
    Console.WriteLine("Before1");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    Console.WriteLine("Original Start");
    if (x == 0)
    {
      goto __aspect_return_2;
    }
    Console.WriteLine("Original End");
    __aspect_return_2:
      Console.WriteLine("After1");
    __aspect_return_1:
      Console.WriteLine("After2");
  }
}