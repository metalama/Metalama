internal class TargetCode
{
  [Aspect]
  private void M()
  {
    this.M_Source();
    this.M_Source();
    this.M_Source();
    return;
  }
  private void M_Source()
  {
  }
}