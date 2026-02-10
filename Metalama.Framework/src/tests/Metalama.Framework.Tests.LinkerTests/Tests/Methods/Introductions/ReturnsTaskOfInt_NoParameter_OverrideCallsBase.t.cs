internal class Target
{
  private async Task<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should return default(int) since this is an awaited base call.
    return default(global::System.Int32);
  }
  public Task<int> Foo()
  {
    return this.Foo_Override();
  }
}
