[IntroductionAttribute]
public interface ITargetInterface
{
  void ExistingMethod();
  static void TestMethod()
  {
    global::System.Console.WriteLine("Default");
  }
}