internal class Target
{
  [Test1]
  [Test2]
  public Target()
  {
    global::System.Console.WriteLine("Contract by aspect 2 on Target.Target(), ordinal 1");
    global::System.Console.WriteLine("Contract by aspect 2 on Target.Target(), ordinal 2");
    global::System.Console.WriteLine("Contract by aspect 1 on Target.Target(), ordinal 1");
    global::System.Console.WriteLine("Contract by aspect 1 on Target.Target(), ordinal 2");
    Console.WriteLine($"Constructor source (no override).");
  }
  [Test1]
  [Override]
  [Test2]
  public Target(int o)
  {
    global::System.Console.WriteLine("Contract by aspect 2 on Target.Target(int), ordinal 1");
    global::System.Console.WriteLine("Contract by aspect 2 on Target.Target(int), ordinal 2");
    global::System.Console.WriteLine("Contract by aspect 1 on Target.Target(int), ordinal 1");
    global::System.Console.WriteLine("Contract by aspect 1 on Target.Target(int), ordinal 2");
    global::System.Console.WriteLine($"Constructor override.");
    Console.WriteLine($"Constructor source (with override).");
  }
}