// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.Target_ClassWithPrimaryConstructor_Template;

#pragma warning disable CS0169 // field is never used

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.BeforeInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        foreach (var fieldOrProperty in meta.Target.Type.FieldsAndProperties)
        {
            if (!fieldOrProperty.IsImplicitlyDeclared)
            {
                fieldOrProperty.Value = fieldOrProperty.Name;
            }
        }
    }
}

// <target>
[Aspect]
internal class TargetCode()
{
    private string? f;
    private string? f1, f2;

    public string? Property1 { get; }

    public string? Property2 { get; set; }
}

#endif