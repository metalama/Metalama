internal class Target
{
  private void Foo()
  {
    Console.WriteLine("Before");
    if (true)
    {
      Console.WriteLine("Original");
    }
    Console.WriteLine("After");
  }
}