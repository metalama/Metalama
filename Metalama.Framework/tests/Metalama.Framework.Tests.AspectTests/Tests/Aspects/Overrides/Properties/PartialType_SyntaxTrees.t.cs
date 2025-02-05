[Override]
internal partial class TargetClass
{
  public int TargetProperty1
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetProperty1.");
      Console.WriteLine("This is TargetProperty1.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetProperty1.");
      Console.WriteLine("This is TargetProperty1.");
      return;
    }
  }
}