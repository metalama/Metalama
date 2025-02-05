internal class TargetCode
{
  [Aspect(Value = MyEnum.B)]
  private int Method(int a)
  {
    global::System.Console.WriteLine("B");
    return a;
  }
}