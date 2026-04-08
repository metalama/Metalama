// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Spec §9 (simplified): Parent-Child wiring via aspect-driven InitializerKind.AfterObjectInitializer.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ParentChild;

[Inheritable]
public class ParentChildAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(WireChildren), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void WireChildren()
    {
        Console.WriteLine( $"Wiring children of {meta.Target.Type.Name}" );
    }
}

[ParentChildAspect]
public class TreeNode
{
    public string Name { get; set; } = "";

    public IReadOnlyList<TreeNode> Children { get; set; } = Array.Empty<TreeNode>();

    public TreeNode? Parent { get; set; }
}

// <target>
public class Caller
{
    public void Method()
    {
        // Nested object initializers — all get .Initialize()
        var tree = new TreeNode
        {
            Name = "root",
            Children = new[]
            {
                new TreeNode { Name = "left" },
                new TreeNode { Name = "right" }
            }
        };
    }
}
