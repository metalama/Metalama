internal partial class TargetClass
{
  [Override]
  public partial int TargetMethod1();
  public partial int TargetMethod2();
  [Override]
  partial void TargetVoidMethodNoImplementation();
  partial void TargetVoidMethodNoImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetVoidMethodNoImplementation().");
    return;
  }
  [Override]
  partial void TargetVoidMethodWithImplementation1();
  partial void TargetVoidMethodWithImplementation2();
}
internal partial class TargetClass
{
  public partial int TargetMethod1()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod1().");
    Console.WriteLine("This is a partial method.");
    return 42;
  }
  [Override]
  public partial int TargetMethod2()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetMethod2().");
    Console.WriteLine("This is a partial method.");
    return 42;
  }
  partial void TargetVoidMethodWithImplementation1()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetVoidMethodWithImplementation1().");
    Console.WriteLine("This is a partial method.");
    return;
  }
  [Override]
  partial void TargetVoidMethodWithImplementation2()
  {
    global::System.Console.WriteLine("This is the override of TargetClass.TargetVoidMethodWithImplementation2().");
    Console.WriteLine("This is a partial method.");
    return;
  }
}