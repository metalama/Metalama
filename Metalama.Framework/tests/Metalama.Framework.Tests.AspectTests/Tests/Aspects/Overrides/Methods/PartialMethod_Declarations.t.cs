[Override]
internal partial class TargetClass
{
  public partial int TargetMethod();
  partial void TargetVoidMethodNoImplementation();
  partial void TargetVoidMethodNoImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetVoidMethodNoImplementation.");
    return;
  }
  partial void TargetVoidMethodWithImplementation();
}
internal partial class TargetClass
{
  public partial int TargetMethod()
  {
    global::System.Console.WriteLine("This is the override of TargetMethod.");
    Console.WriteLine("This is a partial method.");
    return 42;
  }
  partial void TargetVoidMethodWithImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetVoidMethodWithImplementation.");
    Console.WriteLine("This is a partial method.");
    return;
  }
}