[Override]
[Introduction]
internal class TargetClass
{
  public async IAsyncEnumerable<int> ExistingMethod_AsyncIterator()
  {
    global::System.Console.WriteLine("Override");
    await foreach (var r in (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.ExistingMethod_AsyncIterator_Source())))
    {
      yield return r;
    }
    yield break;
  }
  private async IAsyncEnumerable<int> ExistingMethod_AsyncIterator_Source()
  {
    Console.WriteLine("Original");
    await Task.Yield();
    yield return 42;
  }
  private async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> IntroducedMethod_AsyncIterator_Introduction()
  {
    global::System.Console.WriteLine("Introduced");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
  }
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> IntroducedMethod_AsyncIterator()
  {
    global::System.Console.WriteLine("Override");
    await foreach (var r in (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.IntroducedMethod_AsyncIterator_Introduction())))
    {
      yield return r;
    }
    yield break;
  }
}