class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    return this.Foo2();
  }
  short Foo2()
  {
    Console.WriteLine("Original");
    return 42;
  }
}