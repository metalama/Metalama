// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using System.IO;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed record OutputPaths( string BaseDirectory, string CompileTimeAssemblyName, int? AlternateOrdinal )
{
    [Memo]
    public string Directory
        => this.AlternateOrdinal == null
            ? this.BaseDirectory
            : $"{this.BaseDirectory}.{this.AlternateOrdinal}";

    [Memo]
    public string Pe => Path.Combine( this.Directory, this.CompileTimeAssemblyName + ".dll" );

    [Memo]
    public string Pdb => Path.Combine( this.Directory, this.CompileTimeAssemblyName + ".pdb" );

    [Memo]
    public string Manifest => Path.Combine( this.Directory, "manifest.json" );

    public OutputPaths WithAlternateOrdinal( int? alternateOrdinal ) => new( this.BaseDirectory, this.CompileTimeAssemblyName, alternateOrdinal );
}