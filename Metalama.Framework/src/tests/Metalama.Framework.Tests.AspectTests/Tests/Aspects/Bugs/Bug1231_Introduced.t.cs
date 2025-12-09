[IntroduceMethodAspect]
internal class TargetCode<T>
{
  public global::System.Collections.Generic.List<T?> GetValues()
  {
    global::System.Console.WriteLine($"Return type: {typeof(global::System.Collections.Generic.List<T>)}");
    return default;
  }
}
