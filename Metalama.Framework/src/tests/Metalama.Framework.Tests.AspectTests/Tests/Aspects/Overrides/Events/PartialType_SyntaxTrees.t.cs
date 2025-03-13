[Override]
internal partial class TargetClass
{
  public event EventHandler TargetEvent1
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent1.");
      Console.WriteLine("This is TargetEvent1.");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent1.");
      Console.WriteLine("This is TargetEvent1.");
      return;
    }
  }
}