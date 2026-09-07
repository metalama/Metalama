[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static event global::System.EventHandler TestEvent
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