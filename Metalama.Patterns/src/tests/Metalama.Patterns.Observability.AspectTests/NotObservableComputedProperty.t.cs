using System;
using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.NotObservableComputedProperty;
[Observable]
public class DateTimeViewModel : INotifyPropertyChanged
{
  private DateTime _dateTime;
  public DateTime DateTime
  {
    get
    {
      return _dateTime;
    }
    set
    {
      if (_dateTime != value)
      {
        _dateTime = value;
        OnPropertyChanged("ObservableMinutesFromNow");
        OnPropertyChanged("DateTime");
      }
    }
  }
  public double ObservableMinutesFromNow => (DateTime.Now - this.DateTime).TotalMinutes;
  [NotObservable]
  public double NotObservableMinutesFromNow => (DateTime.Now - this.DateTime).TotalMinutes;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}