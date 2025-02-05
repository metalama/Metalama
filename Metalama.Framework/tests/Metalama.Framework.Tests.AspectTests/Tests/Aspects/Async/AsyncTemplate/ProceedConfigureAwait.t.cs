public class TargetClass
{
  protected async Task OnTransactionMethodSuccessAsync()
  {
    await Task.Yield();
  }
  [TransactionalMethod]
  public async Task<int> DoSomethingAsync()
  {
    var result = await this.DoSomethingAsync_Source().ConfigureAwait(false);
    await this.OnTransactionMethodSuccessAsync();
    return (global::System.Int32)result;
  }
  private async Task<int> DoSomethingAsync_Source()
  {
    await Task.Yield();
    Console.WriteLine("Hello");
    return 42;
  }
  [TransactionalMethod]
  public async Task DoSomethingAsync2()
  {
    await this.DoSomethingAsync2_Source().ConfigureAwait(false);
    object result = null;
    await this.OnTransactionMethodSuccessAsync();
    return;
  }
  private async Task DoSomethingAsync2_Source()
  {
    await Task.Yield();
    Console.WriteLine("Hello");
  }
}