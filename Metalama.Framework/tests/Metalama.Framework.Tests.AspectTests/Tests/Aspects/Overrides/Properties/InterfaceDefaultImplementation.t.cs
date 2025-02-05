public interface InterfaceB : InterfaceA
{
  [Override]
  int InterfaceA.PropertyA
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
  }
  [Override]
  int PropertyB
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
  }
}
public class TargetClass : InterfaceB
{
}