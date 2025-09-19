internal class TargetClass
{
  [Override]
  private int Method()
  {
    static void LocalFunction()
    {
      _ = (global::System.Int32)42;
    }
    LocalFunction();
    return default;
  }
  [Override]
  private int Method_ExpressionBody()
  {
    static void LocalFunction()
    {
      _ = (global::System.Int32)42;
    }
    LocalFunction();
    return default;
  }
}