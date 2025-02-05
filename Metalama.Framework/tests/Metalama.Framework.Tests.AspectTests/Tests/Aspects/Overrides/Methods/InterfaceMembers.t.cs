public interface Interface
{
  [Override]
  private int PrivateMethod()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original implementation");
    return 42;
  }
  [Override]
  public static int StaticMethod()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original implementation");
    return 42;
  }
}
public class TargetClass : Interface
{
}