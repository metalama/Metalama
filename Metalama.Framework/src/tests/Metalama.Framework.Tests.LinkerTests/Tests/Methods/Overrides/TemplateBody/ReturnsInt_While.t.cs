internal class Target
{
  private int Foo(int x)
  {
    Console.WriteLine("Before");
    var i = 0;
    var k = 0;
    while (i < 0)
    {
      int result;
      Console.WriteLine("Original");
      result = x;
      k += result;
      i++;
    }
    Console.WriteLine("After");
    return k;
  }
}