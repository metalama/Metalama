[Aspect]
public class TargetCode
{
  static TargetCode()
  {
    Invoke(_ =>
    {
      return "Hello, world.";
    });
  }
  public static void Invoke(Func<object, string> action)
  {
  }
}