[Introduction]
internal class TargetClass
{
  public global::System.Int32 Property_NameConflict
  {
    get
    {
      var Property_NameConflict_1 = $"{Property_NameConflict}";
      return (global::System.Int32)(Property_NameConflict + Property_NameConflict_1.Length);
    }
    set
    {
      var Property_NameConflict_1 = $"{Property_NameConflict}";
      Property_NameConflict = value + Property_NameConflict_1.Length;
    }
  }
  public global::System.Int32 Property_ValueConflict
  {
    get
    {
      return (global::System.Int32)42;
    }
    set
    {
      string value_1 = value.ToString();
    }
  }
}