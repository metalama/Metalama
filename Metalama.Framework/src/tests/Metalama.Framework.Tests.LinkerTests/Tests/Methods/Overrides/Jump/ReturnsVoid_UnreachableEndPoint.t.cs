class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Before2");
    if (x == 0)
    {
      Console.WriteLine("Before1");
      if (x == 0)
      {
        Console.WriteLine("Original Start");
        if (x == 0)
        {
          Console.WriteLine("Branch End");
        }
        else
        {
          Console.WriteLine("Branch End");
        }
        goto __aspect_return_1;
      }
      Console.WriteLine("After1");
      __aspect_return_1:
        return;
    }
    Console.WriteLine("After2");
  }
}