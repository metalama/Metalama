[Aspect]
internal class TargetCode
{
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> DeclarativelyMethodAsync()
  {
    await global::System.Threading.Tasks.Task.Yield();
    yield return 1;
  }
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> ProgrammaticallyMethodAsync()
  {
    await global::System.Threading.Tasks.Task.Yield();
    yield return 1;
  }
}