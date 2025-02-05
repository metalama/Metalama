class Target
{
  int Foo(int x)
  {
    Console.WriteLine("Before");
    int result;
    Console.WriteLine("Original");
    result = x;
    Console.WriteLine("After");
    return result;
  }
}