[Override]
[Introduction]
internal class TargetClass
{
  public T ExistingMethod_Generic<T>(T x)
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original");
    return x;
  }
  public int ExistingMethod_Expression(int x)
  {
    global::System.Console.WriteLine("Override");
    return x;
  }
  public async Task<int> ExistingMethod_TaskAsync()
  {
    global::System.Console.WriteLine("Override");
    return (await this.ExistingMethod_TaskAsync_Source());
  }
  private async Task<int> ExistingMethod_TaskAsync_Source()
  {
    Console.WriteLine("Original");
    await Task.Yield();
    return 42;
  }
  public async void ExistingMethod_VoidAsync()
  {
    global::System.Console.WriteLine("Override");
    await this.ExistingMethod_VoidAsync_Source();
    return;
  }
  private async global::System.Threading.Tasks.ValueTask ExistingMethod_VoidAsync_Source()
  {
    Console.WriteLine("Original");
    await Task.Yield();
  }
  public IEnumerable<int> ExistingMethod_Iterator()
  {
    global::System.Console.WriteLine("Override");
    return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.ExistingMethod_Iterator_Source());
  }
  private IEnumerable<int> ExistingMethod_Iterator_Source()
  {
    Console.WriteLine("Original");
    yield return 42;
  }
  public global::System.Int32 IntroducedMethod_Expression(global::System.Int32 x)
  {
    global::System.Console.WriteLine("Override");
    return x;
  }
  public T IntroducedMethod_Generic<T>(T x)
  {
    global::System.Console.WriteLine("Override");
    global::System.Console.WriteLine("Original");
    return x;
  }
  private global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedMethod_Iterator_Introduction()
  {
    global::System.Console.WriteLine("Introduced");
    yield return 42;
  }
  public global::System.Collections.Generic.IEnumerable<global::System.Int32> IntroducedMethod_Iterator()
  {
    global::System.Console.WriteLine("Override");
    return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.IntroducedMethod_Iterator_Introduction());
  }
  private async global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskAsync_Introduction()
  {
    global::System.Console.WriteLine("Introduced");
    await global::System.Threading.Tasks.Task.Yield();
    return (global::System.Int32)42;
  }
  public async global::System.Threading.Tasks.Task<global::System.Int32> IntroducedMethod_TaskAsync()
  {
    global::System.Console.WriteLine("Override");
    return (await this.IntroducedMethod_TaskAsync_Introduction());
  }
  private async global::System.Threading.Tasks.ValueTask IntroducedMethod_VoidAsync_Introduction()
  {
    global::System.Console.WriteLine("Introduced");
    await global::System.Threading.Tasks.Task.Yield();
  }
  public async void IntroducedMethod_VoidAsync()
  {
    global::System.Console.WriteLine("Override");
    await this.IntroducedMethod_VoidAsync_Introduction();
    return;
  }
}