[Observable]
public class FieldKeyword_SetterSideEffect : INotifyPropertyChanged
{
  // Semi-automatic property whose manual setter has a side effect (regression test for #1644).
  // The aspect must execute the original setter body, not just assign the raw value to the backing field.
  public int Value
  {
    get
    {
      return Value_Source;
    }
    set
    {
      if (Value_Source != value)
      {
        Value_Source = value;
        OnPropertyChanged("Value");
      }
    }
  }
  private int Value_Source
  {
    get => field;
    set
    {
      field = value;
      this.SideEffect++;
    }
  }
  private int _sideEffect;
  public int SideEffect
  {
    get
    {
      return _sideEffect;
    }
    private set
    {
      if (_sideEffect != value)
      {
        _sideEffect = value;
        OnPropertyChanged("SideEffect");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}