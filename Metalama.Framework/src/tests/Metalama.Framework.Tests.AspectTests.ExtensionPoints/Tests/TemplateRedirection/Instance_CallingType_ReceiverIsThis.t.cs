[Redirect]
internal class Grid : Control
{
  public void Refresh()
  {
    this.Update_Interceptor(1);
    this.Update_Interceptor(2);
  }
  public static void TestMain() => new Grid().Refresh();
  private void Update_Interceptor(global::System.Int32 value)
  {
    global::System.Console.WriteLine("Updating.");
    this.Update(value);
    return;
  }
}