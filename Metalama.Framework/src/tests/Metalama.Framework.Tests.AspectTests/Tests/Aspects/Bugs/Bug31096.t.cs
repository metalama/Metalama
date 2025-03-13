public class TargetClass
{
  [TestAspect]
  public void TestMethod1()
  {
    var td = new TestData()
    {
      Property = TestData.TryParse1("42", out var result) ? result : null
    };
    object result_1 = null;
    var result1 = 42;
    var result2 = 42;
    var result3 = 42;
    global::System.Console.WriteLine("Aspect" + result1 + result2 + result3);
    return;
  }
  [TestAspect]
  public void TestMethod2()
  {
    var td = new TestData()
    {
      Property = TestData.TryParse2("42", out var result) ? result.x : null
    };
    object result_1 = null;
    var result1 = 42;
    var result2 = 42;
    var result3 = 42;
    global::System.Console.WriteLine("Aspect" + result1 + result2 + result3);
    return;
  }
  [TestAspect]
  public void TestMethod3()
  {
    var(result1, (result2, result3)) = (42, (42, 42));
    object result = null;
    var result1_1 = 42;
    var result2_1 = 42;
    var result3_1 = 42;
    global::System.Console.WriteLine("Aspect" + result1_1 + result2_1 + result3_1);
    return;
  }
  [TestAspect]
  public void TestMethod4()
  {
    var(result1, (result2, result3)) = (42, (42, 42));
    object result = null;
    var result1_1 = 42;
    var result2_1 = 42;
    var result3_1 = 42;
    global::System.Console.WriteLine("Aspect" + result1_1 + result2_1 + result3_1);
    return;
  }
  [TestAspect]
  public void TestMethod5()
  {
    var(result1, (result2, result3)) = (42, (42, 42));
    object result = null;
    var result1_1 = 42;
    var result2_1 = 42;
    var result3_1 = 42;
    global::System.Console.WriteLine("Aspect" + result1_1 + result2_1 + result3_1);
    return;
  }
}