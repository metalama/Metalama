[Introduction]
internal class TargetClass
{
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> IntroducedMethod_AsyncEnumerable()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
    await foreach (var x in global::Metalama.Framework.RunTime.AsyncEnumerableArray<global::System.Int32>.Empty)
    {
      yield return x;
    }
  }
  public async global::System.Collections.Generic.IAsyncEnumerator<global::System.Int32> IntroducedMethod_AsyncEnumerator()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
    var enumerator = global::Metalama.Framework.RunTime.AsyncEnumerableArray<global::System.Int32>.Empty.GetAsyncEnumerator();
    while (await enumerator.MoveNextAsync())
    {
      yield return enumerator.Current;
    }
  }
}
