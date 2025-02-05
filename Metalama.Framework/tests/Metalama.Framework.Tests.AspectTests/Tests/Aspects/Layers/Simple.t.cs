internal class C
{
  [MyAspect]
  public void M()
  {
    global::System.Console.WriteLine("Layer: Second");
    global::System.Console.WriteLine("Layer: ");
    return;
  }
}