public class C<T>
{
  private int _x;
  public C(int x)
  {
    this._x = x;
  }
  [TheAspect]
  public void operator +=(C<T> value)
  {
    global::System.Console.WriteLine("Overridden.");
    this._x += value._x;
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
    var c = new C<T>(1);
    Console.WriteLine(c += new C<T>(2));
  }
}