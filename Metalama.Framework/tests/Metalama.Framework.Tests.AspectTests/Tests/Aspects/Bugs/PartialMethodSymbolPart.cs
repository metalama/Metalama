namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.PartialMethodSymbolPart;

// <target>
internal partial class TargetCode
{
    partial void M();

    partial void M()
    {
    }
}