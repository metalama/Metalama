[RegistryStorage("Animals")]
internal class Animals
{
  private int _turtles;
  public int Turtles
  {
    get
    {
      var value = global::Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Company\\Product\\Animals", "Turtles", null);
      if (value != null)
      {
        return (global::System.Int32)global::System.Convert.ChangeType(value, typeof(global::System.Int32));
      }
      else
      {
        return default(global::System.Int32);
      }
    }
    set
    {
      this._turtles = value;
    }
  }
}