// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestPublic.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestPublic.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestPublic.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestPublic.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestInternal.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestInternal.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestProtected.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestProtected.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestProtectedInternal.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestProtectedInternal.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestPrivateProtected.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestPrivateProtected.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestProtected.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestProtected.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal global::System.Int32 TestInternal { get; set; }
    private protected global::System.Int32 TestPrivateProtected { get; set; }
    protected global::System.Int32 TestProtected { get; set; }
    protected internal global::System.Int32 TestProtectedInternal { get; set; }
    global::System.Int32 TestPublic { get; set; }
  }
}