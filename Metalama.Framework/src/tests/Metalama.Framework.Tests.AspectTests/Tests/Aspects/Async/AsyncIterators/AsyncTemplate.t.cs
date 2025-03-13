internal class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> AsyncEnumerable(int a)
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Before AsyncEnumerable");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerable_Source(a)));
    global::System.Console.WriteLine("After AsyncEnumerable");
    await global::System.Threading.Tasks.Task.Yield();
    await foreach (var r in result)
    {
      yield return r;
    }
    yield break;
  }
  private async IAsyncEnumerable<int> AsyncEnumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    await Task.Yield();
    Console.WriteLine("Yield 2");
    yield return 2;
    await Task.Yield();
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public async IAsyncEnumerable<int> AsyncEnumerableCancellable(int a, [EnumeratorCancellation] CancellationToken token)
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Before AsyncEnumerableCancellable");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerableCancellable_Source(a, token), token));
    global::System.Console.WriteLine("After AsyncEnumerableCancellable");
    await global::System.Threading.Tasks.Task.Yield();
    await foreach (var r in result)
    {
      yield return r;
    }
    yield break;
  }
  private async IAsyncEnumerable<int> AsyncEnumerableCancellable_Source(int a, [EnumeratorCancellation] CancellationToken token)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    await Task.Yield();
    Console.WriteLine("Yield 2");
    yield return 2;
    await Task.Yield();
    Console.WriteLine("Yield 3");
    yield return 3;
  }
  [Aspect]
  public async IAsyncEnumerator<int> AsyncEnumerator(int a)
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Before AsyncEnumerator");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerator_Source(a)));
    global::System.Console.WriteLine("After AsyncEnumerator");
    await global::System.Threading.Tasks.Task.Yield();
    await using (result)
    {
      while (await result.MoveNextAsync())
      {
        yield return result.Current;
      }
    }
    yield break;
  }
  private async IAsyncEnumerator<int> AsyncEnumerator_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    await Task.Yield();
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    await Task.Yield();
    yield return 3;
  }
  [Aspect]
  public async IAsyncEnumerator<int> AsyncEnumeratorCancellable(int a, CancellationToken token)
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Before AsyncEnumeratorCancellable");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumeratorCancellable_Source(a, token)));
    global::System.Console.WriteLine("After AsyncEnumeratorCancellable");
    await global::System.Threading.Tasks.Task.Yield();
    await using (result)
    {
      while (await result.MoveNextAsync())
      {
        yield return result.Current;
      }
    }
    yield break;
  }
  private async IAsyncEnumerator<int> AsyncEnumeratorCancellable_Source(int a, CancellationToken token)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    await Task.Yield();
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    await Task.Yield();
    yield return 3;
  }
}