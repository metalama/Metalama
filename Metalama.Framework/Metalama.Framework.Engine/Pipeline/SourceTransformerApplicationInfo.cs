// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System.Collections.Immutable;
using System.Reflection;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Provide application information stored using <see cref="AssemblyMetadataAttribute"/>.
/// </summary>
internal sealed class SourceTransformerApplicationInfo : ApplicationInfoBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceTransformerApplicationInfo"/> class.
    /// </summary>
    public SourceTransformerApplicationInfo( bool isLongRunningProcess )
        : base( typeof(SourceTransformerApplicationInfo).Assembly )
    {
        this.IsLongRunningProcess = isLongRunningProcess;
    }

    /// <inheritdoc />
    public override ProcessKind ProcessKind => ProcessKind.Compiler;

    /// <inheritdoc />
    public override bool IsLongRunningProcess { get; }

    /// <inheritdoc />
    public override string Name => "Metalama.Framework";

    public override ImmutableArray<IComponentInfo> Components => ImmutableArray<IComponentInfo>.Empty;

    public override bool IsLicenseAuditEnabled => true;
}