public class Bug
{
  [TaskException]
  public Task<(int a, int b)> Method()
  {
    try
    {
      return Task.FromResult((1, 2));
    }
    catch (global::System.Exception __ex)
    {
      return (global::System.Threading.Tasks.Task<(global::System.Int32 a, global::System.Int32 b)>)System.Threading.Tasks.Task.FromException<(global::System.Int32 a, global::System.Int32 b)>(__ex);
    }
  }
}