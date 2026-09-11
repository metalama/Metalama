// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A delegate that the callback leaves unconfigured is internal, returns void and takes no parameter.
        builder.IntroduceDelegate( "DefaultDelegate" );

        builder.IntroduceDelegate(
            "ValueChangedHandler",
            buildDelegate: d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "sender", builder.Target );
                d.AddParameter( "oldValue", typeof(object) );
                d.AddParameter( "newValue", typeof(object) );
            } );

        builder.IntroduceDelegate(
            "Predicate",
            buildDelegate: d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Boolean );
                d.AddParameter( "value", typeof(int) );
            } );

        // The Invoke method and the constructor of a delegate are synthesized by the compiler from the declaration,
        // so they are registered in the code model and are absent from the output below. Section 4.2 of
        // Metalama.Framework/docs/future/introducing-types.md states the rule.
    }
}

// <target>
[Introduction]
public class TargetType { }
