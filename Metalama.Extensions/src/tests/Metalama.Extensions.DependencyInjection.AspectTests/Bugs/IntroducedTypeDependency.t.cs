[IntroducedTypeDependencyAspect]
public class TargetClass
{
  public TargetClass([AspectGenerated] TargetClassCompanion? targetClassCompanion = default)
  {
    this._targetClassCompanion = targetClassCompanion;
  }
  private TargetClassCompanion _targetClassCompanion;
}