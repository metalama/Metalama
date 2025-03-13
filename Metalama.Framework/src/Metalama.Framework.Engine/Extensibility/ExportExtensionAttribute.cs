// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Extensibility;

[AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
public sealed class ExportExtensionAttribute : Attribute
{
    internal Type ExtensionType { get; }

    internal ExtensionKind ExtensionKind { get; }

    public ExportExtensionAttribute( Type extensionType, ExtensionKind extensionKind )
    {
        this.ExtensionType = extensionType;
        this.ExtensionKind = extensionKind;
    }
}

public enum ExtensionKind
{
    Default,
    DesignTime
}