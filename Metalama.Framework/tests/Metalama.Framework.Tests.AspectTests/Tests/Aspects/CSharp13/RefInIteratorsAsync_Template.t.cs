[TheAspect]
class Target
{
  private async global::System.Threading.Tasks.Task Async()
  {
    await global::System.Threading.Tasks.Task.Yield();
    ref int r = ref (new int[1])[0];
    global::System.Span<global::System.Int32> s = stackalloc int[1];
    await global::System.Threading.Tasks.Task.Yield();
  }
  private async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> AsyncIterator()
  {
    await global::System.Threading.Tasks.Task.Yield();
    yield return 1;
    ref int r = ref (new int[1])[0];
    global::System.Span<global::System.Int32> s = stackalloc int[1];
    await global::System.Threading.Tasks.Task.Yield();
    yield return 2;
  }
  private global::System.Collections.Generic.IEnumerable<global::System.Int32> Iterator()
  {
    yield return 1;
    ref int r = ref (new int[1])[0];
    global::System.Span<global::System.Int32> s = stackalloc int[1];
    yield return 2;
  }
}