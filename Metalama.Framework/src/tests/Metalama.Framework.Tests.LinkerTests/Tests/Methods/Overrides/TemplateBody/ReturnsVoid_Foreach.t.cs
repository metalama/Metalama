class Target
{
  void Foo()
  {
    Console.WriteLine("Before");
    foreach (var i in new[]
    {
      1,
      2,
      3,
      4,
      5
    }
    )
    {
      Console.WriteLine("Original");
    }
    Console.WriteLine("After");
  }
}