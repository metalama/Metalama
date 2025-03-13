private int Method(int a)
{
  object? result = null;
  global::Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.AnonymousMethod.RunTimeClass.Execute(delegate
  {
    result = this.Method(a);
  });
  return (global::System.Int32)result;
}