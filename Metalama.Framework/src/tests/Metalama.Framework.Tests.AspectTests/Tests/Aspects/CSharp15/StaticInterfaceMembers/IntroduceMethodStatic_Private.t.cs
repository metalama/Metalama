[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal static void TestInternal()
    {
      global::System.Console.WriteLine("Internal");
    }
    private static void TestPrivate()
    {
      global::System.Console.WriteLine("Private");
    }
  }
}