// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeLens;

[JsonObject]
public sealed class CodeLensDetailsTable : ICodeLensDetailsTable
{
    public ImmutableArray<CodeLensDetailsHeader> Headers { get; }

    public ImmutableArray<CodeLensDetailsEntry> Entries { get; }

    public static CodeLensDetailsTable Empty { get; } = new( ImmutableArray<CodeLensDetailsHeader>.Empty, ImmutableArray<CodeLensDetailsEntry>.Empty );

    [JsonConstructor]
    public CodeLensDetailsTable( ImmutableArray<CodeLensDetailsHeader> headers, ImmutableArray<CodeLensDetailsEntry> entries )
    {
        this.Headers = headers;
        this.Entries = entries;
    }

    ICodeLensDetailsHeader[] ICodeLensDetailsTable.Headers => this.Headers.ToArray<ICodeLensDetailsHeader>();

    ICodeLensDetailsEntry[] ICodeLensDetailsTable.Entries => this.Entries.ToArray<ICodeLensDetailsEntry>();

    internal static CodeLensDetailsTable CreateError( params string[] messages )
    {
        return new CodeLensDetailsTable(
            ImmutableArray.Create( new CodeLensDetailsHeader( "Error", "Error", true, 1 ) ),
            ImmutableArray.Create( new CodeLensDetailsEntry( messages.SelectAsImmutableArray( m => new CodeLensDetailsField( m ) ) ) ) );
    }
}