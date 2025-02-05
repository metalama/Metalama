internal class TargetCode
{
  [Aspect]
  private void Method()
  {
    global::System.Console.WriteLine("called template T=System.Int32 i=1");
    global::System.Console.WriteLine($"called template 2 T={typeof(global::System.Int32)}");
    global::System.Console.WriteLine($"called template 2 T={typeof(global::System.Int32[])}");
    global::System.Console.WriteLine($"called template 2 T={typeof(global::System.Collections.Generic.Dictionary<global::System.Int32, global::System.Int32>)}");
    global::System.Console.WriteLine($"called template 2 T={typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_DynamicCall.TargetCode)}");
    return;
  }
}