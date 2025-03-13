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
      await this.AsyncVoidMethod_Source();
      return;
    }
    catch
    {
      return;
    }
  }
  private async global::System.Threading.Tasks.ValueTask AsyncVoidMethod_Source()
  {
    await Task.Yield();
    throw new InvalidOperationException();
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
      await this.AsyncTaskMethod_Source();
      return;
    }
    catch
    {
      return;
    }
  }
  private async Task AsyncTaskMethod_Source()
  {
    await Task.Yield();
    throw new InvalidOperationException();
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
      return (await this.AsyncTaskIntMethod_Source());
    }
    catch
    {
      return default(global::System.Int32);
    }
  }
  private async Task<int> AsyncTaskIntMethod_Source()
  {
    await Task.Yield();
    throw new InvalidOperationException();
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
      await this.AsyncValueTaskMethod_Source();
      return;
    }
    catch
    {
      return;
    }
  }
  private async ValueTask AsyncValueTaskMethod_Source()
  {
    await Task.Yield();
    throw new InvalidOperationException();
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
      return (await this.AsyncValueTaskIntMethod_Source());
    }
    catch
    {
      return default(global::System.Int32);
    }
  }
  private async ValueTask<int> AsyncValueTaskIntMethod_Source()
  {
    await Task.Yield();
    throw new InvalidOperationException();
  }
}