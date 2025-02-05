internal class TargetCode
{
  [Aspect]
  public Task<int> TaskReturningNonAsync(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Threading.Tasks.Task<global::System.Int32> result;
    result = Task.FromResult(a);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<global::System.Int32>)result;
  }
  [Aspect]
  public ValueTask<int> ValueTaskReturningNonAsync(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Threading.Tasks.ValueTask<global::System.Int32> result;
    result = new ValueTask<int>(0);
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.ValueTask<global::System.Int32>)result;
  }
  [Aspect]
  public Task<TResult?> GenericTaskReturningNonAsync<TResult, TInput>(TInput x)
  {
    global::System.Console.WriteLine("Before");
    global::System.Threading.Tasks.Task<TResult?> result;
    result = Task.FromResult(default(TResult));
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<TResult?>)result;
  }
  [Aspect]
  public Task<TResult?> GenericConstraintsTaskReturningNonAsync<TResult, TInput>(TInput x)
    where TResult : IDisposable where TInput : IDisposable
  {
    global::System.Console.WriteLine("Before");
    global::System.Threading.Tasks.Task<TResult?> result;
    x.Dispose();
    result = Task.FromResult(default(TResult));
    global::System.Console.WriteLine("After");
    return (global::System.Threading.Tasks.Task<TResult?>)result;
  }
}