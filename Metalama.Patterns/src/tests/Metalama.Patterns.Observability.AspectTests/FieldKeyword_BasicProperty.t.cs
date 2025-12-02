[Observable]
public class FieldKeyword_BasicProperty : INotifyPropertyChanged
{
  private string _name = default !;
  // Semi-automatic property with basic field keyword usage.
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
        OnPropertyChanged("Name");
      }
    }
  }
  private string Name_Source { get => field; set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value)); }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
