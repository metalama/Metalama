// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterIntroduction;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var objectType = TypeFactory.GetType( typeof(object) );

        builder.IntroduceMethod(
            nameof(this.Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "Nullable";
                methodBuilder.Parameters[0].Name = "nullableArg1";
                methodBuilder.Parameters[0].Type = objectType.ToNullable();
                methodBuilder.Parameters[1].Name = "nullableArg2";
                methodBuilder.Parameters[1].Type = objectType.ToNullable();
            } );

        builder.IntroduceMethod(
            nameof(this.Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "NonNullable";
                methodBuilder.Parameters[0].Name = "nonNullableArg1";
                methodBuilder.Parameters[0].Type = objectType.ToNonNullable();
                methodBuilder.Parameters[1].Name = "nonNullableArg2";
                methodBuilder.Parameters[1].Type = objectType.ToNonNullable();
            } );

        builder.IntroduceMethod(
            nameof(this.Template),
            buildMethod: methodBuilder =>
            {
                methodBuilder.Name = "Default";
                methodBuilder.Parameters[0].Name = "defaultArg1";
                methodBuilder.Parameters[0].Type = objectType;
                methodBuilder.Parameters[1].Name = "defaultArg2";
                methodBuilder.Parameters[1].Type = objectType;
            } );
    }

    [Template]
    private string Template( dynamic? arg1, dynamic? arg2 ) => arg1?.ToString() + arg2!.ToString();
}

// <target>
[Aspect]
internal class TargetCode { }