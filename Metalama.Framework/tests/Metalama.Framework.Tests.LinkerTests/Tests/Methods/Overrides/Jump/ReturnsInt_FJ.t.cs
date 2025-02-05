class Target
{
  int Foo(int x)
  {
    Console.WriteLine("Before");
    int result;
    Console.WriteLine("Original Start");
    if (x == 0)
    {
      result = 42;
      goto __aspect_return_1;
    }
    Console.WriteLine("Original End");
    result = x;
    goto __aspect_return_1;
    __aspect_return_1:
      Console.WriteLine("After");
    return result;
  }
}