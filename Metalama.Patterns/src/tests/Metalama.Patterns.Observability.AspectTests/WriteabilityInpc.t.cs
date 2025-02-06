using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.WriteabilityInpc;
[Observable]
public class Person : INotifyPropertyChanged
{
  private string? _firstName;
  public string? FirstName
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
        OnPropertyChanged("FirstName");
      }
    }
  }
  private string? _lastName;
  public string? LastName
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
        OnPropertyChanged("LastName");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelField : INotifyPropertyChanged
{
  private Person _model1 = default !;
  private Person _model
  {
    get
    {
      return _model1;
    }
    set
    {
      if (!object.ReferenceEquals(value, _model1))
      {
        var oldValue = _model1;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handle_modelPropertyChanged;
        }
        _model1 = value;
        OnPropertyChanged("FirstName");
        OnPropertyChanged("FullName");
        OnPropertyChanged("LastName");
        SubscribeTo_model(value);
      }
    }
  }
  public PersonViewModelField(Person model)
  {
    this._model = model;
  }
  public string? FirstName => this._model.FirstName;
  public string? LastName => this._model.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  private PropertyChangedEventHandler? _handle_modelPropertyChanged;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeTo_model(Person value)
  {
    if (value != null)
    {
      _handle_modelPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handle_modelPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "FirstName":
            OnPropertyChanged("FirstName");
            OnPropertyChanged("FullName");
            break;
          case "LastName":
            OnPropertyChanged("FullName");
            OnPropertyChanged("LastName");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelReadonlyField : INotifyPropertyChanged
{
  private readonly Person _model1 = default !;
  private Person _model
  {
    get
    {
      return _model1;
    }
    init
    {
      if (!object.ReferenceEquals(value, _model1))
      {
        var oldValue = _model1;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handle_modelPropertyChanged;
        }
        _model1 = value;
        SubscribeTo_model(value);
      }
    }
  }
  public PersonViewModelReadonlyField(Person model)
  {
    this._model = model;
  }
  public string? FirstName => this._model.FirstName;
  public string? LastName => this._model.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  private PropertyChangedEventHandler? _handle_modelPropertyChanged;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeTo_model(Person value)
  {
    if (value != null)
    {
      _handle_modelPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handle_modelPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "FirstName":
            OnPropertyChanged("FirstName");
            OnPropertyChanged("FullName");
            break;
          case "LastName":
            OnPropertyChanged("FullName");
            OnPropertyChanged("LastName");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelConst : INotifyPropertyChanged
{
  private const Person? _model = null;
  public string? FirstName => _model?.FirstName;
  public string? LastName => _model?.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelGetOnlyProperty : INotifyPropertyChanged
{
  private readonly Person _model = default !;
  private Person Model
  {
    get
    {
      return _model;
    }
    init
    {
      if (!object.ReferenceEquals(value, _model))
      {
        var oldValue = _model;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleModelPropertyChanged;
        }
        _model = value;
        SubscribeToModel(value);
      }
    }
  }
  public PersonViewModelGetOnlyProperty(Person model)
  {
    this.Model = model;
  }
  public string? FirstName => this.Model.FirstName;
  public string? LastName => this.Model.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  private PropertyChangedEventHandler? _handleModelPropertyChanged;
  [ObservedExpressions("Model")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("Model")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToModel(Person value)
  {
    if (value != null)
    {
      _handleModelPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleModelPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "FirstName":
            OnPropertyChanged("FirstName");
            OnPropertyChanged("FullName");
            break;
          case "LastName":
            OnPropertyChanged("FullName");
            OnPropertyChanged("LastName");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelInitProperty : INotifyPropertyChanged
{
  private readonly Person _model = default !;
  private Person Model
  {
    get
    {
      return _model;
    }
    init
    {
      if (!object.ReferenceEquals(value, _model))
      {
        var oldValue = _model;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleModelPropertyChanged;
        }
        _model = value;
        SubscribeToModel(value);
      }
    }
  }
  public PersonViewModelInitProperty(Person model)
  {
    this.Model = model;
  }
  public string? FirstName => this.Model.FirstName;
  public string? LastName => this.Model.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  private PropertyChangedEventHandler? _handleModelPropertyChanged;
  [ObservedExpressions("Model")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("Model")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToModel(Person value)
  {
    if (value != null)
    {
      _handleModelPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleModelPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "FirstName":
            OnPropertyChanged("FirstName");
            OnPropertyChanged("FullName");
            break;
          case "LastName":
            OnPropertyChanged("FullName");
            OnPropertyChanged("LastName");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public class PersonViewModelProperty : INotifyPropertyChanged
{
  private Person _model = default !;
  private Person Model
  {
    get
    {
      return _model;
    }
    set
    {
      if (!object.ReferenceEquals(value, _model))
      {
        var oldValue = _model;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleModelPropertyChanged;
        }
        _model = value;
        OnObservablePropertyChanged("Model", oldValue, (INotifyPropertyChanged? )value);
        OnPropertyChanged("FirstName");
        OnPropertyChanged("FullName");
        OnPropertyChanged("LastName");
        SubscribeToModel(value);
      }
    }
  }
  public PersonViewModelProperty(Person model)
  {
    this.Model = model;
  }
  public string? FirstName => this.Model.FirstName;
  public string? LastName => this.Model.LastName;
  public string FullName => $"{this.FirstName} {this.LastName}";
  private PropertyChangedEventHandler? _handleModelPropertyChanged;
  [ObservedExpressions("Model")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("Model")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToModel(Person value)
  {
    if (value != null)
    {
      _handleModelPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleModelPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "FirstName":
            OnPropertyChanged("FirstName");
            OnPropertyChanged("FullName");
            break;
          case "LastName":
            OnPropertyChanged("FullName");
            OnPropertyChanged("LastName");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}