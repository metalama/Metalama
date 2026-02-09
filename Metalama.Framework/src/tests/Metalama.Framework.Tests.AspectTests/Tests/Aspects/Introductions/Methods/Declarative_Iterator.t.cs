[Introduction]
internal class TargetClass
{
  public global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedMethod_Enumerable()
  {
    global::System.Console.WriteLine("This is introduced method.");
    yield return 42;
    foreach (var x in global::System.Linq.Enumerable.Empty<global::System.Int32>())
    {
      yield return x;
    }
  }
  public global::System.Collections.Generic.IEnumerator<global::System.Int32> IntroducedMethod_Enumerator()
  {
    global::System.Console.WriteLine("This is introduced method.");
    yield return 42;
    var enumerator = global::System.Linq.Enumerable.Empty<global::System.Int32>().GetEnumerator();
    while (enumerator.MoveNext())
    {
      yield return enumerator.Current;
    }
  }
}
