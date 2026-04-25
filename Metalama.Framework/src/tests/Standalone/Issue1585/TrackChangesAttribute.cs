using Metalama.Framework.Aspects;

namespace Issue1585;

// Simplified stand-in for the Track Changes aspect from the Metalama samples.
// Introduces IsChanged / AcceptChanges / RejectChanges on the target class.
public class TrackChangesAttribute : TypeAspect
{
    [Introduce]
    public bool IsChanged { get; private set; }

    [Introduce]
    public void AcceptChanges() => this.IsChanged = false;

    [Introduce]
    public void RejectChanges() => this.IsChanged = false;
}
