// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterIntroduction;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var objectType = TypeFactory.GetType( typeof(object) );

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "Nullable";
                methodBuilder.Parameters.Single().Type = objectType.ToNullable();
            } );

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "NonNullable";
                methodBuilder.Parameters.Single().Type = objectType.ToNonNullable();
            } );

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "Default";
                methodBuilder.Parameters.Single().Type = objectType;
            } );
    }

    [Template]
    private string Template( dynamic? arg ) => arg?.ToString() + arg!.ToString();
}

// <target>
[Aspect]
internal class TargetCode { }