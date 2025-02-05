[EnumViewModel]
internal class VisibilityViewModel
{
  private Visibility _value;
  public VisibilityViewModel(Visibility value)
  {
    _value = value;
  }
  public bool IsCollapsed
  {
    get
    {
      return _value == Visibility.Collapsed;
    }
  }
  public bool IsHidden
  {
    get
    {
      return _value == Visibility.Hidden;
    }
  }
  public bool IsVisible
  {
    get
    {
      return _value == Visibility.Visible;
    }
  }
}