internal class Target
{
  private void Foo(int x)
  {
    Console.WriteLine("Aspect");
    switch (x)
    {
      case 1:
        break;
      // ReSharper disable once RedundantEmptySwitchSection
      default:
        break;
    }
  }
}