class TargetClass
{
  int Method(int x)
  {
    Console.WriteLine("Override2 Begin");
    if (x > 0)
    {
      return LocalFunction1();
    }
    Console.WriteLine("Override2 End");
    return 0;
    int LocalFunction1()
    {
      Console.WriteLine("Override2 Local Function Begin");
      if (x > 0)
      {
        Console.WriteLine("Override1 Begin");
        if (x > 0)
        {
          return LocalFunction1();
        }
        Console.WriteLine("Override1 End");
        return 0;
        int LocalFunction1()
        {
          Console.WriteLine("Override1 Local Function Begin");
          if (x > 0)
          {
            Console.WriteLine("Original Begin");
            return x + 1;
            ;
          }
          Console.WriteLine("Override1 Local Function End");
          return 0;
        }
      }
      Console.WriteLine("Override2 Local Function End");
      return 0;
    }
  }
}