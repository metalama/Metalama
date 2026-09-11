internal class Target
{
  private void Foo(int x)
  {
    if (x == 2)
    {
      goto myLabel;
    }
    Console.WriteLine("Before aspect");
    if (x == 1)
    {
      goto myLabel_2;
    }
    Console.WriteLine("Original start");
    myLabel_2:
      Console.WriteLine("Original end");
    myLabel:
      Console.WriteLine("After aspect");
  }
}