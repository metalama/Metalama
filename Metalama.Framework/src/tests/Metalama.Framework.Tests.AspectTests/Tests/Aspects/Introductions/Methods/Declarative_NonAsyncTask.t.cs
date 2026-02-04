[Introduction]
internal class TargetClass
{
  public global::System.Threading.Tasks.Task IntroducedMethod_Task()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Threading.Tasks.Task);
  }
  public global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskInt()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Threading.Tasks.Task<global::System.Int32>);
  }
}
