// Final Compilation.Emit failed.
// Error CS0126  on `return`: `An object of a type convertible to 'Task' is required`
// Error CS0030  on `(global::System.Threading.Tasks.Task<global::System.Int32>)default(global::System.Int32)`: `Cannot convert type 'int' to 'System.Threading.Tasks.Task<int>'`
// Error CS0126  on `return`: `An object of a type convertible to 'ValueTask' is required`
// Error CS0030  on `(global::System.Threading.Tasks.ValueTask<global::System.Int32>)default(global::System.Int32)`: `Cannot convert type 'int' to 'System.Threading.Tasks.ValueTask<int>'`
// Error CS1626  on `yield`: `Cannot yield a value in the body of a try block with a catch clause`
// Error CS0186  on `default(global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32>)`: `Use of null is not valid in this context`
// Error CS1631  on `yield`: `Cannot yield a value in the body of a catch clause`
// Error CS1626  on `yield`: `Cannot yield a value in the body of a try block with a catch clause`
// Error CS0186  on `default(global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32>)`: `Use of null is not valid in this context`
// Error CS1631  on `yield`: `Cannot yield a value in the body of a catch clause`
public class TestClass
{
  [IgnoreException]
  public Task TaskMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return;
    }
  }
  [IgnoreException]
  public Task<int> TaskIntMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return (global::System.Threading.Tasks.Task<global::System.Int32>)default(global::System.Int32);
    }
  }
  [IgnoreException]
  public ValueTask ValueTaskMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return;
    }
  }
  [IgnoreException]
  public ValueTask<int> ValueTaskIntMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return (global::System.Threading.Tasks.ValueTask<global::System.Int32>)default(global::System.Int32);
    }
  }
  [IgnoreException]
  public async IAsyncEnumerable<int> IAsyncEnumerableMethod()
  {
    try
    {
      await foreach (var r in this.IAsyncEnumerableMethod_Source())
      {
        yield return r;
      }
      yield break;
    }
    catch
    {
      await foreach (var r_1 in default(global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32>))
      {
        yield return r_1;
      }
      yield break;
    }
  }
  private IAsyncEnumerable<int> IAsyncEnumerableMethod_Source()
  {
    throw new InvalidOperationException();
  }
  [IgnoreException]
  public async IAsyncEnumerable<int> AsyncIAsyncEnumerableMethod()
  {
    try
    {
      await foreach (var r in (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncIAsyncEnumerableMethod_Source())))
      {
        yield return r;
      }
      yield break;
    }
    catch
    {
      await foreach (var r_1 in default(global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32>))
      {
        yield return r_1;
      }
      yield break;
    }
  }
  private async IAsyncEnumerable<int> AsyncIAsyncEnumerableMethod_Source()
  {
    await Task.Yield();
    yield return 42;
    throw new InvalidOperationException();
  }
}