using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.LabeledLoopInGetter;
[Observable]
public class ViewModel : INotifyPropertyChanged
{
  private int _threshold;
  public int Threshold
  {
    get
    {
      return _threshold;
    }
    set
    {
      if (_threshold != value)
      {
        _threshold = value;
        OnPropertyChanged("FirstValueOverThreshold");
        OnPropertyChanged("Threshold");
      }
    }
  }
  private int _count;
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
        OnPropertyChanged("FirstValueOverThreshold");
        OnPropertyChanged("Count");
      }
    }
  }
  public int FirstValueOverThreshold
  {
    get
    {
      var result = 0;
      outer:
        for (var i = 0; i < this.Count; i++)
        {
          for (var j = i; j < this.Count; j++)
          {
            if (j <= this.Threshold)
            {
              continue outer;
            }
            result = j;
            if (result > this.Threshold)
            {
              break outer;
            }
          }
        }
      return result;
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}