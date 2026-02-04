[Introduction]
internal class TargetClass
{
  public async global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskInt()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    return default(global::System.Int32);
  }
  public async global::System.Threading.Tasks.Task IntroducedMethod_TaskVoid()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
  }
  public async void IntroducedMethod_Void()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
  }
}
