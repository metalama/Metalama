internal class TargetClass
{
  [Test]
  private int M(int i, int j) => i + j;
  public global::System.Func<global::System.Object?, global::System.Object? [], global::System.Object?> GetMethodInvokerDelegate()
  {
    return (global::System.Func<global::System.Object?, global::System.Object? [], global::System.Object?>)Invoke;
    object? Invoke(object? instance, object? [] args)
    {
      return this.M((global::System.Int32)args[0]!, (global::System.Int32)args[1]!);
    }
  }
}