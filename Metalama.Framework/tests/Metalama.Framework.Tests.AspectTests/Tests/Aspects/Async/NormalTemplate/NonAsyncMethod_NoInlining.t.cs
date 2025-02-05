internal class TargetCode
{
  [Aspect]
  public Task<int> TaskReturningNonAsync(int a)
  {
    global::System.Console.WriteLine("Before");
    var result1 = this.TaskReturningNonAsync_Source(a);
    var result2 = this.TaskReturningNonAsync_Source(a);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<global::System.Int32>)result2;
  }
  private Task<int> TaskReturningNonAsync_Source(int a)
  {
    return Task.FromResult(a);
  }
  [Aspect]
  public ValueTask<int> ValueTaskReturningNonAsync(int a)
  {
    global::System.Console.WriteLine("Before");
    var result1 = this.ValueTaskReturningNonAsync_Source(a);
    var result2 = this.ValueTaskReturningNonAsync_Source(a);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.ValueTask<global::System.Int32>)result2;
  }
  private ValueTask<int> ValueTaskReturningNonAsync_Source(int a)
  {
    return new ValueTask<int>(0);
  }
  [Aspect]
  public Task<TResult?> GenericTaskReturningNonAsync<TResult, TInput>(TInput x)
  {
    global::System.Console.WriteLine("Before");
    var result1 = this.GenericTaskReturningNonAsync_Source<TResult, TInput>(x);
    var result2 = this.GenericTaskReturningNonAsync_Source<TResult, TInput>(x);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<TResult?>)result2;
  }
  private Task<TResult?> GenericTaskReturningNonAsync_Source<TResult, TInput>(TInput x)
  {
    return Task.FromResult(default(TResult));
  }
  [Aspect]
  public Task<TResult?> GenericConstraintsTaskReturningNonAsync<TResult, TInput>(TInput x)
    where TResult : IDisposable where TInput : IDisposable
  {
    global::System.Console.WriteLine("Before");
    var result1 = this.GenericConstraintsTaskReturningNonAsync_Source<TResult, TInput>(x);
    var result2 = this.GenericConstraintsTaskReturningNonAsync_Source<TResult, TInput>(x);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<TResult?>)result2;
  }
  private Task<TResult?> GenericConstraintsTaskReturningNonAsync_Source<TResult, TInput>(TInput x)
    where TResult : IDisposable where TInput : IDisposable
  {
    x.Dispose();
    return Task.FromResult(default(TResult));
  }
}