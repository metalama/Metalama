internal partial class TargetClass
{
  public int TargetProperty3
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetProperty3.");
      Console.WriteLine("This is TargetProperty3.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetProperty3.");
      Console.WriteLine("This is TargetProperty3.");
      return;
    }
  }
}