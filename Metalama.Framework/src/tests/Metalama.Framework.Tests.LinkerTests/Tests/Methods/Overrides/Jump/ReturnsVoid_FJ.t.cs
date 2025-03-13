class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Before");
    Console.WriteLine("Original Start");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    Console.WriteLine("Original End");
    __aspect_return_1:
      Console.WriteLine("After");
  }
}