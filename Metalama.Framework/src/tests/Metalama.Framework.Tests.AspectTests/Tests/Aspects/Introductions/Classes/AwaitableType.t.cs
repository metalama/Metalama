[IntroductionAttribute]
[OverrideAttribute]
public class TargetType
{
  private global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitable GetValue_Introduction()
  {
    return default;
  }
  public async global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitable GetValue()
  {
    global::System.Console.WriteLine("Override");
    await this.GetValue_Introduction();
    return;
  }
  [global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitableMethodBuilder))]
  public class MyAwaitable
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaiter GetAwaiter()
    {
      return default;
    }
  }
  public class MyAwaitableMethodBuilder
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitable Task
    {
      get
      {
        return default;
      }
    }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : global::System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : global::System.Runtime.CompilerServices.IAsyncStateMachine
    {
    }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : global::System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : global::System.Runtime.CompilerServices.IAsyncStateMachine
    {
    }
    public static global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitableMethodBuilder Create()
    {
      return default;
    }
    public void SetException(global::System.Exception exception)
    {
    }
    public void SetResult()
    {
    }
    public void SetStateMachine(global::System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)
    {
    }
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
      where TStateMachine : global::System.Runtime.CompilerServices.IAsyncStateMachine
    {
    }
  }
  public class MyAwaiter : global::System.Runtime.CompilerServices.INotifyCompletion
  {
    public global::System.Boolean IsCompleted
    {
      get
      {
        return (global::System.Boolean)true;
      }
    }
    public void GetResult()
    {
    }
    public void OnCompleted(global::System.Action continuation)
    {
    }
  }
}
