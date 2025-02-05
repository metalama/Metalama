[Introduction]
internal class TargetClass
{
  public global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedMethod_Enumerable()
  {
    global::System.Console.WriteLine("This is introduced method.");
    yield return 42;
    foreach (var x in this.IntroducedMethod_Enumerable_Empty())
    {
      yield return x;
    }
  }
  private global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedMethod_Enumerable_Empty()
  {
    yield break;
  }
  public global::System.Collections.Generic.IEnumerator<global::System.Int32> IntroducedMethod_Enumerator()
  {
    global::System.Console.WriteLine("This is introduced method.");
    yield return 42;
    var enumerator = this.IntroducedMethod_Enumerator_Empty();
    while (enumerator.MoveNext())
    {
      yield return enumerator.Current;
    }
  }
  private global::System.Collections.Generic.IEnumerator<global::System.Int32> IntroducedMethod_Enumerator_Empty()
  {
    yield break;
  }
}