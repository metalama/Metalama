[TestAspect]
public class Target
{
  public int _field;
  public int GetAutoProperty { get; }
  public int InitAutoProperty { get; init; }
  private int _autoProperty;
  public int AutoProperty
  {
    get
    {
      return _autoProperty;
    }
    set
    {
      if (value != _autoProperty)
      {
        _autoProperty = value;
      }
    }
  }
  public int Property
  {
    get
    {
      return Property_Source;
    }
    set
    {
      if (value != Property_Source)
      {
        Property_Source = value;
      }
    }
  }
  private int Property_Source { get => _field; set => _field = value; }
}