// Final Compilation.Emit failed.
// Error CS0815 on `result = await global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedCustomConfigureAwait.TaskExtensions.NoContext(this.DoSomethingAsync2_Source())`: `Cannot assign void to an implicitly-typed variable`
public class TargetClass
{
  protected async Task OnTransactionMethodSuccessAsync()
  {
    await Task.Yield();
  }
  [TransactionalMethod]
  public async Task<int> DoSomethingAsync()
  {
    var result = await global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedCustomConfigureAwait.TaskExtensions.NoContext(this.DoSomethingAsync_Source());
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
    var result = await global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedCustomConfigureAwait.TaskExtensions.NoContext(this.DoSomethingAsync2_Source());
    await this.OnTransactionMethodSuccessAsync();
    return;
  }
  private async Task DoSomethingAsync2_Source()
  {
    await Task.Yield();
    Console.WriteLine("Hello");
  }
}