[Override]
internal partial class TargetClass
{
  public partial int TargetMethod();
  partial void TargetVoidMethodNoImplementation();
  partial void TargetVoidMethodNoImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetVoidMethodNoImplementation.");
    this.TargetVoidMethodNoImplementation_Source();
    this.TargetVoidMethodNoImplementation_Source();
    return;
  }
  private void TargetVoidMethodNoImplementation_Source()
  {
  }
  partial void TargetVoidMethodWithImplementation();
}
internal partial class TargetClass
{
  public partial int TargetMethod()
  {
    global::System.Console.WriteLine("This is the override of TargetMethod.");
    _ = this.TargetMethod_Source();
    return this.TargetMethod_Source();
  }
  private int TargetMethod_Source()
  {
    Console.WriteLine("This is a partial method.");
    return 42;
  }
  partial void TargetVoidMethodWithImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetVoidMethodWithImplementation.");
    this.TargetVoidMethodWithImplementation_Source();
    this.TargetVoidMethodWithImplementation_Source();
    return;
  }
  private void TargetVoidMethodWithImplementation_Source()
  {
    Console.WriteLine("This is a partial method.");
  }
}