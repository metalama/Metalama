public class C
{
  private int _x;
  [TheAspect]
  public void operator +=(int value)
  {
    global::System.Console.WriteLine("Overridden.");
    this._x += value;
    return;
  }
  [TheAspect]
  public void operator ++()
  {
    global::System.Console.WriteLine("Overridden.");
    this._x++;
    return;
  }
  public void M()
  {
    var c = new C();
    Console.WriteLine(c += 5);
  }
}