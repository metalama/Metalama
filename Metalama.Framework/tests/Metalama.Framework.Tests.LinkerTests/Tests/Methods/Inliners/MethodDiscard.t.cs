class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    Console.WriteLine("Original");
    _ = (global::System.Int32)42;
    Console.WriteLine("After");
    return 42;
  }
}