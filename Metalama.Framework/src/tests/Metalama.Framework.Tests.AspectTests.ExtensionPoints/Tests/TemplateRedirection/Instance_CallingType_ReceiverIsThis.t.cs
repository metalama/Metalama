[Redirect]
internal class Grid : Control
{
  public void Refresh()
  {
    this.Update_Interceptor(1);
    this.Update_Interceptor(2);
  }
  internal static void RefreshOthers(Grid grid, Grid? missing, SubGrid subGrid)
  {
    grid.Update_Interceptor(3);
    missing?.Update_Interceptor(4);
    subGrid.Update_Interceptor(5);
  }
  private void Update_Interceptor(global::System.Int32 value)
  {
    global::System.Console.WriteLine("Updating.");
    this.Update(value);
    return;
  }
}