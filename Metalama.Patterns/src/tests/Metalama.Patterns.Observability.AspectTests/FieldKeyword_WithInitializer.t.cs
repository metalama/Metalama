[Observable]
public class FieldKeyword_WithInitializer : INotifyPropertyChanged
{
  // Semi-automatic property with field keyword and initializer.
  public string Name
  {
    get
    {
      return Name_Source;
    }
    set
    {
      if (!object.ReferenceEquals(value, Name_Source))
      {
        Name_Source = value;
        OnPropertyChanged("Display");
        OnPropertyChanged("Name");
      }
    }
  }
  private string Name_Source { get => field; set => field = value.Trim(); } = "Default";
  // Semi-automatic property with value type, validation, and initializer.
  public int Count
  {
    get
    {
      return Count_Source;
    }
    set
    {
      if (Count_Source != value)
      {
        Count_Source = value;
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