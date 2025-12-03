[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroducedByPropertyId
  {
    get
  {
    return (global::System.Int32)42;
  }
    set
    {
      global::System.Console.WriteLine("Hey");
    }
}
  public void IntroducedByMethodId()
  {
    global::System.Console.WriteLine("Method template called.");
  }
}