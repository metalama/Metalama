internal class Target
{
  [TheAspect]
  private void M()
  {
    var m_1 = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.MethodGroupNaturalType.C().M;
    m_1();
    var m = new C().M;
    m();
    return;
  }
}