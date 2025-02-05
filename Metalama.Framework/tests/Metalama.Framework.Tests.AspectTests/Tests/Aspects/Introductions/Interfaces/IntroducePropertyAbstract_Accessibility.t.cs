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