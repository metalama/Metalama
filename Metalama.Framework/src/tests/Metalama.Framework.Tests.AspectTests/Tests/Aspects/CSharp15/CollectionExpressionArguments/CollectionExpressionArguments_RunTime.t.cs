internal class TargetType
{
  [TheAspect]
  public void Method()
  {
    global::System.Collections.Generic.HashSet<global::System.String> set = [with (global::System.StringComparer.OrdinalIgnoreCase), "a", "A"];
    global::System.Console.WriteLine(set.Count);
    return;
  }
}