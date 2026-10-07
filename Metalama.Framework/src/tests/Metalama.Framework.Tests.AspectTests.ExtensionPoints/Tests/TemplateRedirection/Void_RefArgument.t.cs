[Redirect]
internal class Program
{
  public static void TestMain()
  {
    var value = 1;
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Void_RefArgument.Program.Increment_Interceptor(ref value);
    Console.WriteLine(value);
  }
  internal static void Increment_Interceptor(ref global::System.Int32 value)
  {
    global::System.Console.WriteLine("Incrementing.");
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Void_RefArgument.Source.Increment(ref value);
  }
}