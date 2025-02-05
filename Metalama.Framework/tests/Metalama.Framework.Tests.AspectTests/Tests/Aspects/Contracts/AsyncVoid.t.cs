public class Class1
{
  public Task Execute_Task([NotNull] Action action)
  {
    if (action == null)
    {
      throw new global::System.ArgumentNullException();
    }
    return Task.CompletedTask;
  }
  public ValueTask Execute_ValueTask([NotNull] Action action)
  {
    if (action == null)
    {
      throw new global::System.ArgumentNullException();
    }
    return new(Task.CompletedTask);
  }
  public async Task ExecuteAsync_Task([NotNull] Action action)
  {
    if (action == null)
    {
      throw new global::System.ArgumentNullException();
    }
    await Task.Yield();
  }
  public async ValueTask ExecuteAsync_ValueTask([NotNull] Action action)
  {
    if (action == null)
    {
      throw new global::System.ArgumentNullException();
    }
    await Task.Yield();
  }
  public async void ExecuteAsync_Void([NotNull] Action action)
  {
    if (action == null)
    {
      throw new global::System.ArgumentNullException();
    }
    await Task.Yield();
  }
}