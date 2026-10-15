[MyAspect]
internal class C
{
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