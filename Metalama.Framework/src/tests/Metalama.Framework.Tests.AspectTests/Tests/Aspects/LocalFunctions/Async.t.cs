internal class C
{
  [Retry]
  private void Foo()
  {
    return;
  }
  [Retry]
  private async Task FooAsync()
  {
    async global::System.Threading.Tasks.Task<global::System.Object?> ExecuteCoreAsync()
    {
      return default;
    }
    await global::System.Threading.Tasks.Task.Run(ExecuteCoreAsync);
    return;
  }
}
