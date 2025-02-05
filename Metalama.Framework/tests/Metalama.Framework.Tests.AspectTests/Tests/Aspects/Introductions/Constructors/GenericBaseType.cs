using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.GenericBaseType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c =>
            {
                var p = c.AddParameter( "p", builder.Target.TypeParameters[0] );
                c.InitializerKind = ConstructorInitializerKind.Base;
                c.AddInitializerArgument( p );
            } );

        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.IsStatic = true; } );
    }

    [Template]
    public void Template() { }
}

internal class BaseClass<T>
{
    public BaseClass() { }

    public BaseClass( T value ) { }
}

// <target>
[Introduction]
internal class TargetClass<T> : BaseClass<T> { }