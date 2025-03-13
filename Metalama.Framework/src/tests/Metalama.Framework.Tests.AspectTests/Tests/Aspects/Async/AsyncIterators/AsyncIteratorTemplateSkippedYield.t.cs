class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> Enumerable(int a)
  {
    global::System.Console.WriteLine("Starting Enumerable");
    throw new global::System.Exception();
    yield break;
  }
  [Aspect]
  public async IAsyncEnumerator<int> Enumerator(int a)
  {
    global::System.Console.WriteLine("Starting Enumerator");
    var enumerator = this.Enumerator_Source(a);
    throw new global::System.Exception();
    yield break;
  }
  private async IAsyncEnumerator<int> Enumerator_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Yield 1");
    yield return 1;
  }
}