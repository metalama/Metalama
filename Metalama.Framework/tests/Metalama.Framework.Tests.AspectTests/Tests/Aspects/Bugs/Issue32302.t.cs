public class C
{
  [MyAspect]
  private void M()
  {
    global::System.Console.WriteLine($"M");
    return;
  }
}