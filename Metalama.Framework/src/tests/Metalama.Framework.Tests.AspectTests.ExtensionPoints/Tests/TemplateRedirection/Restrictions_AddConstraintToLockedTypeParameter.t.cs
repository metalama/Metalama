[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_AddConstraintToLockedTypeParameter.Program.Echo_Interceptor<global::System.Int32>(1));
  internal static T Echo_Interceptor<T>(T value)
    where T : struct, global::System.IComparable
  {
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_AddConstraintToLockedTypeParameter.Source.Echo<T>(value);
  }
}