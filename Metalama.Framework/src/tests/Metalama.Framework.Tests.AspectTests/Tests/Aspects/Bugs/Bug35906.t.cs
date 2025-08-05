public class TestClass
{
  [TestAspect(IntMarkerType = typeof(TheOneICareAboutAttribute))]
  public int LogNumber()
  {
    return (global::System.Int32)0;
    return 0;
  }
  private class TheOneICareAboutAttribute : Attribute
  {
  }
}