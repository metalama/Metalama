[Introduction]
internal class TargetClass
{
  public event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Get");
    }
    remove
    {
      global::System.Console.WriteLine("Set");
    }
  }
  public event global::System.EventHandler? EventField;
  public event global::System.EventHandler EventFromAccessors
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
}