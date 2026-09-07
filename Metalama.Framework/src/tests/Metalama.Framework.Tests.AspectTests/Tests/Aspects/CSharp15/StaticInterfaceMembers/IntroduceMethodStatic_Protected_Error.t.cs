// Final Compilation.Emit failed.
// Error CS8707 on `TestPrivateProtected`: `Target runtime doesn't support 'protected', 'protected internal', or 'private protected' accessibility for a member of an interface.`
// Error CS8707 on `TestProtected`: `Target runtime doesn't support 'protected', 'protected internal', or 'private protected' accessibility for a member of an interface.`
// Error CS8707 on `TestProtectedInternal`: `Target runtime doesn't support 'protected', 'protected internal', or 'private protected' accessibility for a member of an interface.`
[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    private protected static void TestPrivateProtected()
    {
      global::System.Console.WriteLine("PrivateProtected");
    }
    protected static void TestProtected()
    {
      global::System.Console.WriteLine("Protected");
    }
    protected internal static void TestProtectedInternal()
    {
      global::System.Console.WriteLine("ProtectedInternal");
    }
  }
}