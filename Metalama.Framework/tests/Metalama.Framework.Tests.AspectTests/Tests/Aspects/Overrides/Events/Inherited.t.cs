[InheritedOverrideEvent]
public event EventHandler? Event
{
  add
  {
    global::System.Console.WriteLine("Add accessor.");
  }
  remove
  {
    global::System.Console.WriteLine("Remove accessor.");
  }
}