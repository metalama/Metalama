[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal void TestInternal()
    {
      global::System.Console.WriteLine("Implementation");
    }
    private protected void TestPrivateProtected()
    {
      global::System.Console.WriteLine("Implementation");
    }
    protected void TestProtected()
    {
      global::System.Console.WriteLine("Implementation");
    }
    protected internal void TestProtectedInternal()
    {
      global::System.Console.WriteLine("Implementation");
    }
    void TestPublic()
    {
      global::System.Console.WriteLine("Implementation");
    }
  }
}