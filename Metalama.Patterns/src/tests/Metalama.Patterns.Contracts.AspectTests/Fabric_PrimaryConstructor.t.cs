using Metalama.Framework.Fabrics;
using System;
namespace Metalama.Patterns.Contracts.AspectTests.Fabric_PrimaryConstructor
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Fabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public sealed class Memento : IDisposable
  {
    private readonly Action _action = default !;
    private Action Action
    {
      get
      {
        return _action;
      }
      init
      {
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The 'Action' property must not be null.");
        }
        _action = value;
      }
    }
    void IDisposable.Dispose()
    {
      this.Action();
    }
    public Memento(Action action)
    {
      Action = action;
      if (action == null !)
      {
        throw new ArgumentNullException("action", "The 'action' parameter must not be null.");
      }
    }
  }
}