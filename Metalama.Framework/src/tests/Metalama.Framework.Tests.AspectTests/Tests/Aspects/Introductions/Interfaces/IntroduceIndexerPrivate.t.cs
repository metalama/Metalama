[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    void TestUsageMethod()
    {
      this[42] = this[42] + 1;
    }
    private global::System.Int32 this[global::System.Int32 index]
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
  }
}