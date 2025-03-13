internal class C
{
  [Retry]
  private void Foo()
  {
    object result = null;
    return;
  }
  [Retry]
  private async Task FooAsync()
  {
    async global::System.Threading.Tasks.Task<global::System.Object?> ExecuteCoreAsync()
    {
      await this.FooAsync_Source();
      object result = null;
      return (global::System.Object? )result;
    }
    await global::System.Threading.Tasks.Task.Run(ExecuteCoreAsync);
    return;
  }
  private async Task FooAsync_Source()
  {
  }
}