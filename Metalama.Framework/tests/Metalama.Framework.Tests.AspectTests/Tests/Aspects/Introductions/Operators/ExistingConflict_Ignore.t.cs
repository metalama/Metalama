[Introduction]
internal class TargetClass
{
  public static TargetClass operator -(TargetClass a)
  {
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
  public static TargetClass operator +(TargetClass a, TargetClass b)
  {
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
  public static explicit operator TargetClass(int a)
  {
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
}