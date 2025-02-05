public interface InterfaceB : InterfaceA
{
  [Override]
  int InterfaceA.MethodA()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Default implementation");
    return 42;
  }
  [Override]
  int MethodB()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Default implementation");
    return 42;
  }
}
public class TargetClass : InterfaceB
{
}