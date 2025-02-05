internal partial class TargetClass
{
  public int TargetProperty2
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetProperty2.");
      Console.WriteLine("This is TargetProperty2.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetProperty2.");
      Console.WriteLine("This is TargetProperty2.");
      return;
    }
  }
}