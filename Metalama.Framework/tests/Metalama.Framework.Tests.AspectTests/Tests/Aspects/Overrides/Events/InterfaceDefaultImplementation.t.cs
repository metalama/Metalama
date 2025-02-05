public interface InterfaceB : InterfaceA
{
  [Override]
  event EventHandler? InterfaceA.EventA
  {
    add
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
    remove
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
  }
  [Override]
  event EventHandler? EventB
  {
    add
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
    remove
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Default implementation");
    }
  }
}
public class TargetClass : InterfaceB
{
}