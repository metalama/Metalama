[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal void TestInternal();
    private protected void TestPrivateProtected();
    protected void TestProtected();
    protected internal void TestProtectedInternal();
    void TestPublic();
  }
}