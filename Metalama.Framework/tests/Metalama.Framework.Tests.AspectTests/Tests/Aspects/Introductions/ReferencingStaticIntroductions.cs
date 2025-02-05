using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Threading;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ReferencingStaticIntroductions;

internal class IdAttribute : TypeAspect
{
    [Introduce]
    private static int _nextId;

    [Introduce]
    public static int Id { get; } = Interlocked.Increment( ref _nextId );

    [Introduce]
    public static void Method( int? id )
    {
        if (id == null)
        {
            Method( Id );
        }
    }
}

// <target>
[Id]
internal class TargetClass { }