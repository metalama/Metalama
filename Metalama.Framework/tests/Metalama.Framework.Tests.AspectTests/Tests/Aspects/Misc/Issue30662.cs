// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30662;

internal class RegisterInstanceAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            "instanceRegistry",
            typeof(IInstanceRegistry),
            TypedConstant.Default( typeof(IInstanceRegistry) ),
            pullAction: ( parameter, constructor ) =>
                PullAction.IntroduceParameterAndPull(
                    "instanceRegistry",
                    TypeFactory.GetType( typeof(IInstanceRegistry) ),
                    TypedConstant.Default( typeof(IInstanceRegistry) ) ) );

        builder.AddInitializer( StatementFactory.Parse( "instanceRegistry.Register( this );" ) );
    }
}

public interface IInstanceRegistry
{
    void Register( object instance );
}

// <target>

internal class Foo
{
    [RegisterInstance]
    public Foo() { }
}

// <target>
internal class Bar : Foo { }