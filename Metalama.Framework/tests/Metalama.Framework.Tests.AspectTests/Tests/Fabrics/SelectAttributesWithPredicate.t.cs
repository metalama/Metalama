[Marker(true)]
public class C
{
  [Marker(false)]
  public void UnmarkedMethod()
  {
  }
  [Marker(true)]
  public void MarkedMethod()
  {
    global::System.Console.WriteLine($"Marked!");
    return;
  }
}