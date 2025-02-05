private int Method(int a)
{
  object? result = null;
  global::Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.LambdaStatement_Parameters.RunTimeClass.Execute(x =>
  {
    global::System.Console.WriteLine(x);
    result = this.Method(a);
  });
  return (global::System.Int32)result;
}