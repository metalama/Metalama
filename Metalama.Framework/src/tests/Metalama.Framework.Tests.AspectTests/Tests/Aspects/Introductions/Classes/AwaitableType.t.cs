[IntroductionAttribute]
[OverrideAttribute]
public class TargetType
{
  public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AwaitableType.TargetType.MyAwaitable GetValue()
  {
    global::System.Console.WriteLine("Override");
    return default;
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
