public interface Interface
{
  [Override]
  private event EventHandler PrivateEvent
  {
    add
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
    remove
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
  }
  [Override]
  public static event EventHandler StaticEvent
  {
    add
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
    remove
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
  }
}
public class TargetClass : Interface
{
}