[Redirect]
internal class Program
{
  public static void TestMain()
  {
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.AddedTypeParameter_NonGenericTemplate.Program.Log_Interceptor<global::System.Int32>(42);
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.AddedTypeParameter_NonGenericTemplate.Program.Log_Interceptor<global::System.String>("text");
  }
  internal static void Log_Interceptor<TArg>(TArg value)
  {
    global::System.Console.WriteLine($"Logging a value of type {typeof(TArg).Name}.");
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.AddedTypeParameter_NonGenericTemplate.Source.Log(value);
  }
}