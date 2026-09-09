internal class TargetType
{
  [TheAspect]
  public void Method(string p)
  {
    global::System.Collections.Generic.List<global::System.String> byCapacity = [with (2), p, "b"];
    global::System.Collections.Generic.HashSet<global::System.String> named = [with (comparer: global::System.StringComparer.OrdinalIgnoreCase), p];
    global::System.Collections.Generic.HashSet<global::System.String> spread = [with (global::System.StringComparer.OrdinalIgnoreCase), ..byCapacity, "A"];
    global::System.Collections.Generic.List<global::System.Collections.Generic.HashSet<global::System.String>> nested = [[with (global::System.StringComparer.OrdinalIgnoreCase), p]];
    global::System.Collections.Generic.HashSet<global::System.String> repeated = [with (global::System.StringComparer.OrdinalIgnoreCase), p];
    global::System.Console.WriteLine("first" + repeated.Count);
    global::System.Collections.Generic.HashSet<global::System.String> repeated_1 = [with (global::System.StringComparer.OrdinalIgnoreCase), p];
    global::System.Console.WriteLine("second" + repeated_1.Count);
    global::System.Collections.Generic.HashSet<global::System.String> conditional = [with (global::System.StringComparer.OrdinalIgnoreCase), p];
    global::System.Console.WriteLine(conditional.Count);
    global::System.Console.WriteLine(byCapacity.Count + named.Count + spread.Count + nested.Count);
    return;
  }
}