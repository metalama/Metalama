internal partial class TargetClass
{
  public event EventHandler TargetEvent2
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent2.");
      Console.WriteLine("This is TargetEvent2.");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent2.");
      Console.WriteLine("This is TargetEvent2.");
      return;
    }
  }
}