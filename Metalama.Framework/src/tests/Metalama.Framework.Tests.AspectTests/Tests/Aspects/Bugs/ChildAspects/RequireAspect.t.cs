private int Method([Aspect1] int a, [Aspect1] string b)
{
  global::System.Console.WriteLine("Aspect1@TargetCode.Method(int, string)/a,Aspect1@TargetCode.Method(int, string)/b");
  return a;
}