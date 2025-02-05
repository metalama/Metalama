#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_UseParameter;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            var p = builder.With( constructor ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 15 ) ).Declaration;
            builder.IntroduceProperty( nameof(Template), tags: new { parameter = p }, buildProperty: b => b.Name = "BuiltProperty" );
        }
    }

    [Template]
    public int Template { get; set; } = ( (IParameter)meta.Tags["parameter"]! ).Value!;
}

public class A( int x )
{
    public int X { get; set; } = x;
}

// <target>
[MyAspect]
public class C( int x ) : A( 42 )
{
    public int Y { get; } = x;
}

#endif