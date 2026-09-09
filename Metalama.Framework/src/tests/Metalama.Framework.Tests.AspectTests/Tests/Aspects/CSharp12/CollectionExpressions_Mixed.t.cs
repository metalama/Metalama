[TheAspect]
public void Method(string p, int i)
{
  global::System.Collections.Generic.List<global::System.String> first = ["Method", p];
  global::System.Console.WriteLine(first.Count);
  global::System.Collections.Generic.List<global::System.String> last = [p, "Method"];
  global::System.Console.WriteLine(last.Count);
  int[] middle = [i, 2, i];
  global::System.Console.WriteLine(middle.Length);
  global::System.Collections.Generic.List<global::System.String> spread = [..new global::System.String[]
  {
    "Method",
    "C"
  }, p];
  global::System.Console.WriteLine(spread.Count);
  return;
}