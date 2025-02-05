[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal event global::System.EventHandler TestInternal;
    private protected event global::System.EventHandler TestPrivateProtected;
    protected event global::System.EventHandler TestProtected;
    protected internal event global::System.EventHandler TestProtectedInternal;
    event global::System.EventHandler TestPublic;
  }
}