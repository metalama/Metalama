// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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