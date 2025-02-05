[TheAspect]
class Program
{
  static void M1(int x) => Console.WriteLine("Old M1");
  [OverloadResolutionPriority(1)]
  static void M1(long x) => Console.WriteLine("New M1");
  static void M2(int x) => Console.WriteLine("Old M2");
  static void M3(int x) => Console.WriteLine("Old M3");
  [global::System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute(1)]
  static void M3(long x) => Console.WriteLine("New M3");
  static void TestMain()
  {
    M1(1);
    M2(2);
    M3(3);
  }
  [global::System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute(1)]
  private static void M2(global::System.Int64 x)
  {
    global::System.Console.WriteLine("New M2");
  }
}