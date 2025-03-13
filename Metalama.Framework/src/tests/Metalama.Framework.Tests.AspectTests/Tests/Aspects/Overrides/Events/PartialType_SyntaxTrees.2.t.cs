internal partial class TargetClass
{
  public event EventHandler TargetEvent3
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent3.");
      Console.WriteLine("This is TargetEvent3.");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent3.");
      Console.WriteLine("This is TargetEvent3.");
      return;
    }
  }
}