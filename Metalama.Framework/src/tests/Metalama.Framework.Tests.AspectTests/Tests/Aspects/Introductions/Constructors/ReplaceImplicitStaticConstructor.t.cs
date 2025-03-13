[Introduction]
internal class TargetClass
{
  public static int F = 42;
  public static int P { get; set; } = 42;
  static TargetClass()
  {
    global::System.Console.WriteLine("Before");
    global::System.Console.WriteLine("After");
  }
}