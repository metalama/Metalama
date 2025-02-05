internal class TargetClass
{
  [Override]
  private int Method()
  {
    void LocalFunction()
    {
      _ = (global::System.Int32)42;
    }
    LocalFunction();
    return default;
  }
  [Override]
  private int Method_ExpressionBody()
  {
    void LocalFunction()
    {
      _ = (global::System.Int32)42;
    }
    LocalFunction();
    return default;
  }
}