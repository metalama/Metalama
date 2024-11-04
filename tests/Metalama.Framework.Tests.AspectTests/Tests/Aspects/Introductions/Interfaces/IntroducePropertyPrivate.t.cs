[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    private global::System.Int32 TestProperty
    {
      get
      {
        global::System.Console.WriteLine("Default");
        return (global::System.Int32)0;
      }
      set
      {
        global::System.Console.WriteLine("Default");
      }
    }
    void TestUsageMethod()
    {
      this.TestProperty = this.TestProperty + 1;
    }
  }
}