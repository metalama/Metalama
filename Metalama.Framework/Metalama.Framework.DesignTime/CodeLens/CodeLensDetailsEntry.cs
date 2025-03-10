// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeLens;

[JsonObject]
public sealed class CodeLensDetailsEntry : ICodeLensDetailsEntry
{
    [JsonConstructor]
    public CodeLensDetailsEntry( ImmutableArray<CodeLensDetailsField> fields, string? tooltip = null )
    {
        this.Fields = fields;
        this.Tooltip = tooltip;
    }

    ICodeLensDetailsField[] ICodeLensDetailsEntry.Fields => this.Fields.ToArray<ICodeLensDetailsField>();

    public ImmutableArray<CodeLensDetailsField> Fields { get; }

    public string? Tooltip { get; }
}