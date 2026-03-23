public class TargetClass : BaseClass
{
  public override void Foo()
  {
    global::System.Console.WriteLine("hello");
    Console.WriteLine("Original");
    return;
  }
}
