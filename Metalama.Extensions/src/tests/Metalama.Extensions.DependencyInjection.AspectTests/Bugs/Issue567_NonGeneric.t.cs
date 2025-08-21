public sealed class ExceptionAwareAttribute : OverrideMethodAspect
{
  [IntroduceDependency(IsRequired = true)]
  [CompiledTemplate(Accessibility = Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private readonly ILastChanceExceptionHandler? _exceptionHandler;
  // ...
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
public abstract class ModelBase
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