class Target
{
  int Foo(int x)
  {
    Console.WriteLine("Before");
    int result = 0;
    if (x == 0)
    {
      Console.WriteLine("Original");
      result = x;
    }
    Console.WriteLine("After");
    return result;
  }
}