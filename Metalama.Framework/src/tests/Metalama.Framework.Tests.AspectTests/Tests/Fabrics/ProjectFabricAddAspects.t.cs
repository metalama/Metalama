internal class TargetCode
{
  private int Method1(int a) => a;
  private string Method2(string s)
  {
    global::System.Console.WriteLine("overridden");
    global::System.Console.WriteLine("Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.ProjectFabricAddAspects.Fabric");
    return s;
  }
}