internal class Target
{
  [LogAspect]
  public static void DoWork()
  {
    Console.WriteLine("Entering DoWork");
    Console.WriteLine("Working...");
    object result = null;
    Console.WriteLine("Leaving DoWork");
  }
}
