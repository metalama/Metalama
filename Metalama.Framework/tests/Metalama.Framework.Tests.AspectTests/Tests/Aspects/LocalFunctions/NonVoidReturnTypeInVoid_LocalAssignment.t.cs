internal class TargetClass
{
  [Override]
  private void Method()
  {
    int LocalFunction()
    {
      Console.WriteLine();
      object x = null;
      return (global::System.Int32)(x?.GetHashCode() ?? 0);
    }
    LocalFunction();
    return;
  }
  [Override]
  private void Method_ExpressionBody()
  {
    int LocalFunction()
    {
      Console.WriteLine();
      object x = null;
      return (global::System.Int32)(x?.GetHashCode() ?? 0);
    }
    LocalFunction();
    return;
  }
}