using System;
using System.ComponentModel;
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Metalama.Patterns.Observability.AspectTests.WriteabilityNonInpc;
[Observable]
public class PersonViewModelField : INotifyPropertyChanged
{
  private string _name1 = default !;
  private string _name
  {
    get
    {
      return _name1;
    }
    set
    {
      if (!object.ReferenceEquals(value, _name1))
      {
        _name1 = value;
        OnPropertyChanged("Description");
        OnPropertyChanged("Name");
      }
    }
  }
  private DateOnly _dateOfBirth1;
  private DateOnly _dateOfBirth
  {
    get
    {
      return _dateOfBirth1;
    }
    set
    {
      if (_dateOfBirth1 != value)
      {
        _dateOfBirth1 = value;
        OnPropertyChanged("DateOfBirth");
      }
    }
  }
  public PersonViewModelField(string name, DateOnly dateOfBirth)
  {
    this._name = name;
    this._dateOfBirth = dateOfBirth;
  }
  public string Name => this._name;
  public DateOnly DateOfBirth => this._dateOfBirth;
  public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelReadonlyField : INotifyPropertyChanged
{
  private readonly string _name;
  private readonly DateOnly _dateOfBirth;
  public PersonViewModelReadonlyField(string name, DateOnly dateOfBirth)
  {
    this._name = name;
    this._dateOfBirth = dateOfBirth;
  }
  public string Name => this._name;
  public DateOnly DateOfBirth => this._dateOfBirth;
  public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelConst : INotifyPropertyChanged
{
  private const string _name = "Adele";
  private const int _yearOfBirth = 1988;
  public string Name => _name;
  public int YearOfBirth => _yearOfBirth;
  public string Description => $"{this.Name} (b. {this.YearOfBirth})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelGetOnlyProperty : INotifyPropertyChanged
{
  private string Name0 { get; }
  private DateOnly DateOfBirth0 { get; }
  public PersonViewModelGetOnlyProperty(string name, DateOnly dateOfBirth)
  {
    this.Name0 = name;
    this.DateOfBirth0 = dateOfBirth;
  }
  public string Name => this.Name0;
  public DateOnly DateOfBirth => this.DateOfBirth0;
  public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelInitProperty : INotifyPropertyChanged
{
  private string Name0 { get; init; }
  private DateOnly DateOfBirth0 { get; init; }
  public PersonViewModelInitProperty(string name, DateOnly dateOfBirth)
  {
    this.Name0 = name;
    this.DateOfBirth0 = dateOfBirth;
  }
  public string Name => this.Name0;
  public DateOnly DateOfBirth => this.DateOfBirth0;
  public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelProperty : INotifyPropertyChanged
{
  private string _name0 = default !;
  private string Name0
  {
    get
    {
      return _name0;
    }
    set
    {
      if (!object.ReferenceEquals(value, _name0))
      {
        _name0 = value;
        OnPropertyChanged("Description");
        OnPropertyChanged("Name");
      }
    }
  }
  private DateOnly _dateOfBirth0;
  private DateOnly DateOfBirth0
  {
    get
    {
      return _dateOfBirth0;
    }
    set
    {
      if (_dateOfBirth0 != value)
      {
        _dateOfBirth0 = value;
        OnPropertyChanged("DateOfBirth");
      }
    }
  }
  public PersonViewModelProperty(string name, DateOnly dateOfBirth)
  {
    this.Name0 = name;
    this.DateOfBirth0 = dateOfBirth;
  }
  public string Name => this.Name0;
  public DateOnly DateOfBirth => this.DateOfBirth0;
  public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}