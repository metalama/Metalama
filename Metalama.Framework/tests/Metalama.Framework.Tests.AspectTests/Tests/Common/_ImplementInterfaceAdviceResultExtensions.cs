using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising;

// This class is used in tests.
[CompileTime]
public static class _ImplementInterfaceAdviceResultExtensions
{
    public static IReadOnlyCollection<IInterfaceMemberImplementationResult> GetObsoleteInterfaceMembers( this IImplementInterfaceAdviceResult result )
#pragma warning disable CS0618 // Type or member is obsolete
        => result.InterfaceMembers;
#pragma warning restore CS0618 // Type or member is obsolete
}