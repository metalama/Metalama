internal class C
{
  [MyAspect]
  public void M()
  {
  }
  public void N()
  {
    global::System.Console.WriteLine("Overridden.");
    return;
  }
  private global::System.Int32 _introducedField;
}