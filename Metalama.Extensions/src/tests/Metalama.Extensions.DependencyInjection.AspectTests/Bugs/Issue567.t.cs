public abstract class ModelBase<T>
{
  [ExceptionAware]
  protected Task Initialize(CancellationToken parameter)
  {
    try
    {
      return Task.CompletedTask;
    }
    catch (Exception e)
    {
      _exceptionHandler.OnException(e);
      throw;
    }
  }
  private ILastChanceExceptionHandler? _exceptionHandler;
  protected ModelBase(ILastChanceExceptionHandler? exceptionHandler = null)
  {
    this._exceptionHandler = exceptionHandler ?? throw new System.ArgumentNullException(nameof(exceptionHandler));
  }
}
public abstract class UpdateModelBase<T> : ModelBase<T> where T : class
{
  private readonly IMutable<T> _last;
  protected UpdateModelBase(ILastChanceExceptionHandler? exceptionHandler = default) : this(new Copied<T>(), exceptionHandler)
  {
  }
  protected UpdateModelBase(IMutable<T> last, ILastChanceExceptionHandler? exceptionHandler = default) : base(exceptionHandler)
  {
    this._last = last;
  }
}