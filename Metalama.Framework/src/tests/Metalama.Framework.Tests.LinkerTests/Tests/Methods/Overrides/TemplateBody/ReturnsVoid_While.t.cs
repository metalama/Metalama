internal class Target
{
  private void Foo()
  {
    Console.WriteLine("Before");
    var i = 0;
    while (i < 5)
    {
      Console.WriteLine("Original");
      i++;
    }
    Console.WriteLine("After");
  }
}