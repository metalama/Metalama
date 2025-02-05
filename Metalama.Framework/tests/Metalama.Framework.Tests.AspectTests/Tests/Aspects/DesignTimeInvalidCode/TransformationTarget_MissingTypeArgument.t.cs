// Error CS7003 on `List<>`: `Unexpected use of an unbound generic name`
public partial class TargetCode
{
  [TestAspect]
  public TargetCode(List<List<>> x, int z, int z2)
  {
  }
}