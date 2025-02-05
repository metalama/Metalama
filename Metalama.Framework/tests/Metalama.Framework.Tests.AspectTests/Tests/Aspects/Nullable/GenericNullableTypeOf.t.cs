internal class C
{
  [MyAspect]
  private string? _f;
  public void Template()
  {
    global::System.IServiceProvider serviceProvider = null !;
    var x = (global::System.String? )serviceProvider.GetService(typeof(global::System.String));
  }
}