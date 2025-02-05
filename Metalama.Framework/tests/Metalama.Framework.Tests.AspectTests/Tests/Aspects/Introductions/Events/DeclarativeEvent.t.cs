[Introduction]
internal class TargetClass
{
  public event global::System.EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("Original add accessor.");
    }
    remove
    {
      global::System.Console.WriteLine("Original remove accessor.");
    }
  }
}