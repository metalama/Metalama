class Target
{
  int Foo(int x)
  {
    Console.WriteLine("Aspect");
    global::System.Int32 z;
    switch (x)
    {
      case 1:
        z = 42;
        break;
      default:
        z = 0;
        break;
    }
    return z;
  }
}