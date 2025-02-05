[Introduction]
internal class TargetClass
{
  public static int operator +(TargetClass x, int y)
  {
    global::System.Int32 z;
    z = x.ToString()!.Length + y;
    return (global::System.Int32)(x.ToString().Length + y);
  }
  public static explicit operator int (TargetClass x)
  {
    global::System.Int32 z;
    z = x.ToString()!.Length + 42;
    return (global::System.Int32)(x.ToString().Length + 42);
  }
}