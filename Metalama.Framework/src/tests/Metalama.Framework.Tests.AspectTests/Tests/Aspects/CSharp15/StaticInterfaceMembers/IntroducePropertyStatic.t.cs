[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static global::System.Int32 TestProperty
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