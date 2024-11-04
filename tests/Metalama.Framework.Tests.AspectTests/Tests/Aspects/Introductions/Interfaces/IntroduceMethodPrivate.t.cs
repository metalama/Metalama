[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    private void TestMethod()
    {
      global::System.Console.WriteLine("Default");
    }
    void TestUsageMethod()
    {
      this.TestMethod();
    }
  }
}