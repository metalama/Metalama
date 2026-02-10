internal class Target
{
  private async Task Foo_Override()
  {
    Console.WriteLine("Before");
  }
  public Task Foo()
  {
    return this.Foo_Override();
  }
}
