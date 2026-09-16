[Observable]
public class C : INotifyPropertyChanged
{
  public Result CurrentResult { get; }
  public object? CurrentValue => this.CurrentResult.Value;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}