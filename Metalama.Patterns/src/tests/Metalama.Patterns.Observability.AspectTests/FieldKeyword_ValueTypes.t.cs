[Observable]
public class FieldKeyword_ValueTypes : INotifyPropertyChanged
{
  private int _age;
  // Semi-automatic property with value type and validation.
  public int Age
  {
    get
    {
      return _age;
    }
    set
    {
      if (_age != value)
      {
        _age = value;
        OnPropertyChanged("Summary");
        OnPropertyChanged("Age");
      }
    }
  }
  private int Age_Source { get => field; set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value)); }
  private double _score;
  // Semi-automatic property with value type and clamping.
  public double Score
  {
    get
    {
      return _score;
    }
    set
    {
      if (_score != value)
      {
        _score = value;
        OnPropertyChanged("Summary");
        OnPropertyChanged("Score");
      }
    }
  }
  private double Score_Source { get => field; set => field = Math.Clamp(value, 0, 100); }
  // Computed property depending on value-type semi-automatic properties.
  public string Summary => $"Age: {this.Age}, Score: {this.Score}";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
