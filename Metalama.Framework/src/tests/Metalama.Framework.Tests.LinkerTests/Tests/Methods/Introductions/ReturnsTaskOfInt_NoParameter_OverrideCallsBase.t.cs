internal class Target
{
  private Task<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should return Task.FromResult(default(int)).
    return global::System.Threading.Tasks.Task.FromResult<global::System.Int32>(default(global::System.Int32));
  }
  public Task<int> Foo()
  {
    return this.Foo_Override();
  }
}
