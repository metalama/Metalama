[Introduction]
internal class TargetClass
{
  public async global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskInt()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    return (global::System.Int32)(await this.IntroducedMethod_TaskInt_Empty());
  }
  private async global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskInt_Empty()
  {
    return default(global::System.Int32);
  }
  public async global::System.Threading.Tasks.Task IntroducedMethod_TaskVoid()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    await this.IntroducedMethod_TaskVoid_Empty();
  }
  private async global::System.Threading.Tasks.Task IntroducedMethod_TaskVoid_Empty()
  {
  }
  public async void IntroducedMethod_Void()
  {
    global::System.Console.WriteLine("This is introduced method.");
    await global::System.Threading.Tasks.Task.Yield();
    await this.IntroducedMethod_Void_Empty();
  }
  private async global::System.Threading.Tasks.ValueTask IntroducedMethod_Void_Empty()
  {
  }
}