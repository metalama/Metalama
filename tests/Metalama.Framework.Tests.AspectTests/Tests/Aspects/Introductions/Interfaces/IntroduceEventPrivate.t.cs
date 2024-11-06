[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    void TestUsageMethod()
    {
      this.TestEvent += (global::System.EventHandler)((s, ea) => global::System.Console.WriteLine("Handler"));
    }
    private event global::System.EventHandler TestEvent
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
  }
}