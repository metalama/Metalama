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
public abstract class UpdateModelBase : ModelBase<string>
{
  // ReSharper disable once NotAccessedField.Local
  private readonly IMutable<string> _last;
  protected UpdateModelBase() : this(new Copied<string>())
  {
  }
  protected UpdateModelBase(IMutable<string> last)
  {
    this._last = last;
  }
}