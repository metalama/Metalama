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