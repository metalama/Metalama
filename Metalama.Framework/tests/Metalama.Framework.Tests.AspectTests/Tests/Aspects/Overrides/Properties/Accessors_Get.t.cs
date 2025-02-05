internal class TargetClass
{
  [Override]
  public int ExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  [Override]
  private int PrivateExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  [Override]
  public static int Static_ExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  [Override]
  public int GetterProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
  }
  [Override]
  private int PrivateGetterProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
  }
  [Override]
  public static int Static_GetterProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return 42;
    }
  }
  [Override]
  public int GetterExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  [Override]
  private int PrivateGetterExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  [Override]
  public int Static_GetterExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
}