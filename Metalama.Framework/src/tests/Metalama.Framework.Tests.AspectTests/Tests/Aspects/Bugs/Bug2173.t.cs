public class C
{
  [TheAspect(Message = "Hello")]
  public void M()
  {
    global::System.Console.WriteLine("Hello");
    return;
  }
  [DerivedAspect(Message = "World")]
  public void N()
  {
    global::System.Console.WriteLine("World");
    return;
  }
}