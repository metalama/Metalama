[Observable]
public class FieldKeyword_ValueTypes : INotifyPropertyChanged
{
  // Semi-automatic property with value type and validation.
  public int Age
  {
    get
    {
      return Age_Source;
    }
    set
    {
      if (Age_Source != value)
      {
        Age_Source = value;
        OnPropertyChanged("Summary");
        OnPropertyChanged("Age");
      }
    }
  }
  private int Age_Source { get => field; set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value)); }
  // Semi-automatic property with value type and clamping.
  public double Score
  {
    get
    {
      return Score_Source;
    }
    set
    {
      if (Score_Source != value)
      {
        Score_Source = value;
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