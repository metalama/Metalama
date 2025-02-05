[NotifyPropertyChanged]
internal class Car : INotifyPropertyChanged
{
  private string? _make;
  public string? Make
  {
    get
    {
      return _make;
    }
    set
    {
      var value_1 = value;
      if (value_1 != _make)
      {
        this.OnPropertyChanged("Make");
        _make = value;
      }
    }
  }
  private double _power;
  public double Power
  {
    get
    {
      return _power;
    }
    set
    {
      var value_1 = value;
      if (value_1 != _power)
      {
        this.OnPropertyChanged("Power");
        _power = value;
      }
    }
  }
  protected virtual void OnPropertyChanged(string name)
  {
    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
  public event PropertyChangedEventHandler PropertyChanged;
}