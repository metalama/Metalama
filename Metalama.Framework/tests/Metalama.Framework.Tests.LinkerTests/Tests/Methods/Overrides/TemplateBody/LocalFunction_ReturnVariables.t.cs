class TargetClass
{
  int Method(int x)
  {
    Console.WriteLine("Override2");
    return LocalFunction2();
    int LocalFunction2()
    {
      Console.WriteLine("Override2 Local Function");
      global::System.Int32 z;
      Console.WriteLine("Override1");
      z = LocalFunction1();
      int LocalFunction1()
      {
        Console.WriteLine("Override1 Local Function");
        global::System.Int32 y;
        Console.WriteLine("Original Begin");
        y = x + 1;
        return y;
      }
      return z;
    }
  }
}