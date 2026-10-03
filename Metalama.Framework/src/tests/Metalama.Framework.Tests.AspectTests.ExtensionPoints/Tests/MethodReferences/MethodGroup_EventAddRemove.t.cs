[Redirect]
internal static class Program
{
  private static event Action? Changed;
  public static void TestMain()
  {
    Changed += global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_EventAddRemove.Interceptors.OnChanged;
    Changed?.Invoke();
    Changed -= global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_EventAddRemove.Interceptors.OnChanged;
    Console.WriteLine(Changed == null);
  }
}