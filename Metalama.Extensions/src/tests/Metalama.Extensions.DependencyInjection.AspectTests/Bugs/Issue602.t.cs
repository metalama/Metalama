public class TargetClass
{
  [Dependency]
  private readonly IServiceA _serviceA;
  [Dependency]
  private readonly IServiceB _serviceB;
  [Dependency]
  private readonly IServiceC _serviceC;
  [Dependency]
  private readonly IServiceD _serviceD;
  [Dependency]
  private readonly IServiceE _serviceE;
  public TargetClass([AspectGenerated] IServiceA? serviceA = null, [AspectGenerated] IServiceB? serviceB = null, [AspectGenerated] IServiceC? serviceC = null, [AspectGenerated] IServiceD? serviceD = null, [AspectGenerated] IServiceE? serviceE = null)
  {
    this._serviceE = serviceE ?? throw new System.ArgumentNullException(nameof(serviceE));
    this._serviceD = serviceD ?? throw new System.ArgumentNullException(nameof(serviceD));
    this._serviceC = serviceC ?? throw new System.ArgumentNullException(nameof(serviceC));
    this._serviceB = serviceB ?? throw new System.ArgumentNullException(nameof(serviceB));
    this._serviceA = serviceA ?? throw new System.ArgumentNullException(nameof(serviceA));
  }
}