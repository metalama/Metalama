[Introduction]
internal class TargetClass
{
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> IntroducedMethod_AsyncEnumerable()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
    await foreach (var x in this.IntroducedMethod_AsyncEnumerable_Empty())
    {
      yield return x;
    }
  }
  private async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> IntroducedMethod_AsyncEnumerable_Empty()
  {
    yield break;
  }
  public async global::System.Collections.Generic.IAsyncEnumerator<global::System.Int32> IntroducedMethod_AsyncEnumerator()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
    var enumerator = this.IntroducedMethod_AsyncEnumerator_Empty();
    while (await enumerator.MoveNextAsync())
    {
      yield return enumerator.Current;
    }
  }
  private async global::System.Collections.Generic.IAsyncEnumerator<global::System.Int32> IntroducedMethod_AsyncEnumerator_Empty()
  {
    yield break;
  }
}