// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ReturnGenericTypeWithGeneratedTypeArgument;

//https://github.com/metalama/Metalama/issues/1049
public class MyAspectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );
        var introducedType = builder.IntroduceClass( "IntroducedType",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
            } ).Declaration;


        var retType = ((INamedType) TypeFactory.GetType( typeof( IEnumerable<> ) ))
            .WithTypeArguments( introducedType );

        builder.IntroduceMethod( nameof( MethodTemplate ),
            buildMethod: m =>
            {
                m.Name = "Method";
                m.ReturnType = retType;
            } );
    }

    [Template]
    public dynamic MethodTemplate()
    {
        return default;
    }
}

// <target>
[MyAspect]
public class MyClass
{
}
