[IntroductionAttribute]
public class TargetType
{
  public async global::System.Threading.Tasks.Task<global::System.Int32> GetValueAsync()
  {
    global::System.Console.WriteLine("Override");
    await global::System.Threading.Tasks.Task.Yield();
    return (global::System.Int32)42;
  }
  public class MyAwaitable
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaiter GetAwaiter()
    {
      return default;
    }
  }
  public class MyAwaiter
  {
    public global::System.Boolean IsCompleted
    {
      get
      {
        return (global::System.Boolean)true;
      }
    }
    public global::System.Int32 GetResult()
    {
      return default;
    }
    public void OnCompleted(global::System.Action continuation)
    {
    }
  }
}
