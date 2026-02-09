internal class Target
{
  private Task Foo_Override()
  {
    Console.WriteLine("Before");
    // Should return Task.CompletedTask.
    return global::System.Threading.Tasks.Task.CompletedTask;
  }
  public Task Foo()
  {
    return this.Foo_Override();
  }
}
