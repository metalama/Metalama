[Observable]
public class FieldKeyword_WithInitializer : INotifyPropertyChanged
{
  private string _name = "Default";
  // Semi-automatic property with field keyword and initializer.
  public string Name
  {
    get
    {
      return _name;
    }
    set
    {
      if (!object.ReferenceEquals(value, _name))
      {
        _name = value;
        OnPropertyChanged("Display");
        OnPropertyChanged("Name");
      }
    }
  }
  private string Name_Source { get => field; set => field = value?.Trim() ?? string.Empty; } = "Default";
  private int _count = 10;
  // Semi-automatic property with value type, validation, and initializer.
  public int Count
  {
    get
    {
      return _count;
    }
    set
    {
      if (_count != value)
      {
        _count = value;
        OnPropertyChanged("Display");
        OnPropertyChanged("Count");
      }
    }
  }
  private int Count_Source { get => field; set => field = Math.Max(value, 0); } = 10;
  // Computed property depending on initialized semi-automatic properties.
  public string Display => $"{this.Name}: {this.Count}";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
