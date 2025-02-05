class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Aspect");
    switch (x)
    {
      case 1:
        break;
      default:
        break;
    }
  }
}