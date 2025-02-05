internal class C
{
  [Aspect]
  private int M(int i) => 42;
  public global::System.Func<global::System.Object?, global::System.Object? [], global::System.Object?> GetMethodInvoker()
  {
    return (global::System.Func<global::System.Object?, global::System.Object? [], global::System.Object?>)Invoke;
    object? Invoke(object? instance, object? [] args)
    {
      return ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Parameter_ArgsArray.C)instance).M((global::System.Int32)args[0]!);
    }
  }
}