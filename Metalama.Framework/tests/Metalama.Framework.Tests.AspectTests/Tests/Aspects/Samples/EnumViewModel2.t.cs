[EnumViewModel]
public class TargetClass
{
  [Flags]
  public enum StringOptions
  {
    None,
    ToUpperCase = 1,
    RemoveSpace = 2,
    Trim = 4
  }
  public class StringOptionsViewModel
  {
    private readonly StringOptions _value;
    public StringOptionsViewModel(StringOptions value)
    {
      _value = value;
    }
    public bool IsNone
    {
      get
      {
        return (_value & StringOptions.None) == StringOptions.None;
      }
    }
    public bool IsRemoveSpace
    {
      get
      {
        return (_value & StringOptions.RemoveSpace) == StringOptions.RemoveSpace;
      }
    }
    public bool IsToUpperCase
    {
      get
      {
        return (_value & StringOptions.ToUpperCase) == StringOptions.ToUpperCase;
      }
    }
    public bool IsTrim
    {
      get
      {
        return (_value & StringOptions.Trim) == StringOptions.Trim;
      }
    }
  }
}