internal partial class TargetClass
{
  [Override1]
  public partial int TargetMethod1();
  public partial int TargetMethod1()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod1().");
    return default;
  }
  [Override2]
  public partial int TargetMethod2();
  public partial int TargetMethod2()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod2().");
    global::System.Int32 result;
    result = default;
    return (global::System.Int32)result;
  }
  [Override3]
  public partial int TargetMethod3();
  public partial int TargetMethod3()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod3().");
    return default;
  }
  [Override1]
  public partial void TargetMethod4();
  public partial void TargetMethod4()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod4().");
    return;
  }
  [Override2]
  public partial void TargetMethod5();
  public partial void TargetMethod5()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod5().");
    object result = null;
    return;
  }
  [Override3]
  public partial void TargetMethod6();
  public partial void TargetMethod6()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod6().");
    return;
  }
}