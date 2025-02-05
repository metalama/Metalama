using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.BaseInitializer;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.InitializerKind = ConstructorInitializerKind.Base; } );

        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c =>
            {
                var p = c.AddParameter( "p", typeof(int) );
                c.InitializerKind = ConstructorInitializerKind.Base;
                c.AddInitializerArgument( p );
            } );
    }

    [Template]
    public void Template() { }
}

internal class BaseClass
{
    public BaseClass() { }

    public BaseClass( int value ) { }
}

// <target>
[Introduction]
internal class TargetClass : BaseClass { }