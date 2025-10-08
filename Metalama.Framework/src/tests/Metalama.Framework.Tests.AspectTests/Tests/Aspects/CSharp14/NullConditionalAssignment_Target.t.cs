internal class C
{
  [TheAspect]
  public void M()
  {
    global::System.Console.WriteLine("Oops");
    this.P?.P = null;
    return;
  }
  public C? P { get; set; }
}