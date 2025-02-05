internal class TargetClass
{
  private readonly IServiceProvider? _serviceProvider;
  [ImportServiceAspect]
  private IFormatProvider? FormatProvider
  {
    get
    {
      return (IFormatProvider? )_serviceProvider.GetService(typeof(IFormatProvider));
    }
    init
    {
      throw new NotSupportedException();
    }
  }
  public string? Format(object? o)
  {
    return ((ICustomFormatter? )FormatProvider?.GetFormat(typeof(ICustomFormatter)))?.Format(null, o, FormatProvider);
  }
}