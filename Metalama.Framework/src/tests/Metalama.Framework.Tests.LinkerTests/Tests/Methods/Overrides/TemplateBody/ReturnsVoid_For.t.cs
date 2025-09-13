internal class Target
{
  private void Foo()
  {
    Console.WriteLine("Before");
    for (var i = 0; i < 5; i++)
    {
      Console.WriteLine("Original");
    }
    Console.WriteLine("After");
  }
}