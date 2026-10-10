// Warning LAMA5162 on `ComputeNorm1`: `The 'VectorHelper.ComputeNorm1(Vector)' method cannot be observed because it is not known to be constant: it is an instance method of a type that is not immutable, or it has a parameter of a type that is not immutable. If the method always returns the same value for the same arguments, mark it with [Constant] or call ConfigureObservability via a fabric.`
using System;
using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.SuppressWarnings2;
[Observable]
public class Vector : INotifyPropertyChanged
{
  private double _x;
  public double X
  {
    get
    {
      return _x;
    }
    set
    {
      if (_x != value)
      {
        _x = value;
        OnPropertyChanged("X");
      }
    }
  }
  private double _y;
  public double Y
  {
    get
    {
      return _y;
    }
    set
    {
      if (_y != value)
      {
        _y = value;
        OnPropertyChanged("Y");
      }
    }
  }
  public double NormWithWarning => VectorHelper.ComputeNorm1(this);
  [SuppressObservabilityWarnings]
  public double NormWithoutWarning => VectorHelper.ComputeNorm2(this);
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
public static class VectorHelper
{
  public static double ComputeNorm1(Vector v) => Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
  public static double ComputeNorm2(Vector v) => Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
}