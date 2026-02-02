public class TestClass
{
  [IgnoreException]
  public void VoidMethod()
  {
    try
    {
      throw new InvalidOperationException();
      return;
    }
    catch
    {
      return;
    }
  }
  [IgnoreException]
  public async void AsyncVoidMethod()
  {
    try
    {
      await Task.Yield();
      throw new InvalidOperationException();
      return;
    }
    catch
    {
      return;
    }
  }
  [IgnoreException]
  public int IntMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return default(global::System.Int32);
    }
  }
  [IgnoreException]
  public Task TaskMethod()
  {
    try
    {
      throw new InvalidOperationException();
    }
    catch
    {
      return default(global::System.Threading.Tasks.Task);
    }
  }
  [IgnoreException]
  public async Task AsyncTaskMethod()
  {
    try
    {
      await Task.Yield();
      throw new InvalidOperationException();
      return;
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
      return default(global::System.Threading.Tasks.Task<global::System.Int32>);
    }
  }
  [IgnoreException]
  public async Task<int> AsyncTaskIntMethod()
  {
    try
    {
      await Task.Yield();
      throw new InvalidOperationException();
    }
    catch
    {
      return default(global::System.Int32);
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
      return default(global::System.Threading.Tasks.ValueTask);
    }
  }
  [IgnoreException]
  public async ValueTask AsyncValueTaskMethod()
  {
    try
    {
      await Task.Yield();
      throw new InvalidOperationException();
      return;
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
      return default(global::System.Threading.Tasks.ValueTask<global::System.Int32>);
    }
  }
  [IgnoreException]
  public async ValueTask<int> AsyncValueTaskIntMethod()
  {
    try
    {
      await Task.Yield();
      throw new InvalidOperationException();
    }
    catch
    {
      return default(global::System.Int32);
    }
  }
}
