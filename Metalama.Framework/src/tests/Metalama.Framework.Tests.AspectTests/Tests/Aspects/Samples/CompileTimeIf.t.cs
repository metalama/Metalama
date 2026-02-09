internal class TargetCode
{
  [CompileTimeIf]
  public void InstanceMethod()
  {
    Console.WriteLine($"Invoking TargetCode.InstanceMethod() on instance {this.ToString()}.");
    Console.WriteLine("InstanceMethod");
  }
  [CompileTimeIf]
  public static void StaticMethod()
  {
    Console.WriteLine("Invoking TargetCode.StaticMethod()");
    Console.WriteLine("StaticMethod");
  }
}