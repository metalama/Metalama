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
  protected ModelBase([AspectGenerated] ILastChanceExceptionHandler? exceptionHandler = null)
  {
    this._exceptionHandler = exceptionHandler ?? throw new System.ArgumentNullException(nameof(exceptionHandler));
  }
}
public abstract class UpdateModelBase<T> : ModelBase<T> where T : class
{
  // ReSharper disable once NotAccessedField.Local
  private readonly IMutable<T> _last;
  protected UpdateModelBase([AspectGenerated] ILastChanceExceptionHandler? exceptionHandler = default) : this(new Copied<T>(), exceptionHandler)
  {
  }
  protected UpdateModelBase(IMutable<T> last, [AspectGenerated] ILastChanceExceptionHandler? exceptionHandler = default) : base(exceptionHandler)
  {
    this._last = last;
  }
}