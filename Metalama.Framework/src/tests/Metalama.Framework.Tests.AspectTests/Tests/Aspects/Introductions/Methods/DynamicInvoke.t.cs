[Introduction]
internal class TargetClass
{
  public int OverrideInt()
  {
    global::System.Console.WriteLine("Introduced");
    return this.OverrideInt_Source();
  }
  private int OverrideInt_Source()
  {
    return 1;
  }
  public void OverrideVoid()
  {
    global::System.Console.WriteLine("Introduced");
    this.OverrideVoid_Source();
  }
  private void OverrideVoid_Source()
  {
  }
  public global::System.Int32 IntroduceInt()
  {
    global::System.Console.WriteLine("Introduced");
    return default(global::System.Int32);
  }
  public void IntroduceVoid()
  {
    global::System.Console.WriteLine("Introduced");
  }
}
