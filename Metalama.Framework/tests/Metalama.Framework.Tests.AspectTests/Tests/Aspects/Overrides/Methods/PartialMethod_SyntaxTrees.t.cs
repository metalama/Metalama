[Override]
internal partial class TargetClass
{
  public partial int TargetMethod();
  partial void TargetMethodNoImplementation();
  partial void TargetMethodNoImplementation()
  {
    global::System.Console.WriteLine("This is the override of TargetMethodNoImplementation.");
    return;
  }
  partial void TargetVoidMethodWithImplementation();
}