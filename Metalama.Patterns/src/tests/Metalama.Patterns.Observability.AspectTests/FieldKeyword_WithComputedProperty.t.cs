[Observable]
public class FieldKeyword_WithComputedProperty : INotifyPropertyChanged
{
  private string _firstName = "";
  // Semi-automatic property with validation.
  public string FirstName
  {
    get
    {
      return _firstName;
    }
    set
    {
      if (!object.ReferenceEquals(value, _firstName))
      {
        _firstName = value;
        OnPropertyChanged("FullName");
        OnPropertyChanged("FirstName");
      }
    }
  }
  private string FirstName_Source { get => field; set => field = value.Trim(); } = "";
  private string _lastName = "";
  // Semi-automatic property with validation.
  public string LastName
  {
    get
    {
      return _lastName;
    }
    set
    {
      if (!object.ReferenceEquals(value, _lastName))
      {
        _lastName = value;
        OnPropertyChanged("FullName");
        OnPropertyChanged("LastName");
      }
    }
  }
  private string LastName_Source { get => field; set => field = value.Trim(); } = "";
  // Computed property that depends on semi-automatic properties.
  public string FullName => $"{this.FirstName} {this.LastName}";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}