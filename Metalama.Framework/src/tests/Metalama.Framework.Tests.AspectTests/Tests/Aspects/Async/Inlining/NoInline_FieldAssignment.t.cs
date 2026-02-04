internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    this._lastResult = await this.AsyncMethod_Source(a);
    global::System.Console.WriteLine("After");
    return (global::System.Int32)this._lastResult;
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  private global::System.Int32 _lastResult;
}
