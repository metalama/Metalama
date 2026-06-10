[Observable]
public class FieldKeyword_WithComputedProperty : INotifyPropertyChanged
{
  // Semi-automatic property with validation.
  public string FirstName
  {
    get
    {
      return FirstName_Source;
    }
    set
    {
      if (!object.ReferenceEquals(value, FirstName_Source))
      {
        FirstName_Source = value;
        OnPropertyChanged("FullName");
        OnPropertyChanged("FirstName");
      }
    }
  }
  private string FirstName_Source { get => field; set => field = value.Trim(); } = "";
  // Semi-automatic property with validation.
  public string LastName
  {
    get
    {
      return LastName_Source;
    }
    set
    {
      if (!object.ReferenceEquals(value, LastName_Source))
      {
        LastName_Source = value;
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