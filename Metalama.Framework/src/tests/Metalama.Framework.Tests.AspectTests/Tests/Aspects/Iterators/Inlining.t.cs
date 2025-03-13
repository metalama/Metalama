internal class TargetCode
{
  [Aspect]
  public IEnumerable<int> Enumerable(int a)
  {
    global::System.Console.WriteLine("Begin Enumerable");
    var x = this.Enumerable_Source(a);
    return (global::System.Collections.Generic.IEnumerable<global::System.Int32>)x;
  }
  private IEnumerable<int> Enumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public IEnumerator<int> Enumerator(int a)
  {
    global::System.Console.WriteLine("Begin Enumerator");
    return this.Enumerator_Source(a);
  }
  private IEnumerator<int> Enumerator_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public IEnumerable OldEnumerable(int a)
  {
    global::System.Console.WriteLine("Begin OldEnumerable");
    var x = this.OldEnumerable_Source(a);
    return (global::System.Collections.IEnumerable)x;
  }
  private IEnumerable OldEnumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public IEnumerator OldEnumerator(int a)
  {
    global::System.Console.WriteLine("Begin OldEnumerator");
    return this.OldEnumerator_Source(a);
  }
  private IEnumerator OldEnumerator_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
}