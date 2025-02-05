internal class TargetCode
{
  private int Method1(int a)
  {
    global::System.Console.WriteLine("overridden");
    global::System.Console.WriteLine("Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.TwoProjectFabrics.Fabric2");
    return a;
  }
  private string Method2(string s)
  {
    global::System.Console.WriteLine("overridden");
    global::System.Console.WriteLine("Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.TwoProjectFabrics.Fabric1");
    return s;
  }
}