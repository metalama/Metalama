internal class TargetCode : BaseClass
{
  [Aspect]
  public override async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    Console.WriteLine("Override");
    return await base.AsyncMethod(a);
  }
}
