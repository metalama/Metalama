// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class ReferencedAssemblyCollection : IAssemblyCollection
{
    private readonly CompilationModel _compilationModel;
    private readonly IModuleSymbol _module;

    public ReferencedAssemblyCollection( CompilationModel compilationModel, IModuleSymbol module )
    {
        this._compilationModel = compilationModel;
        this._module = module;
    }

    public IEnumerator<IAssembly> GetEnumerator()
    {
        foreach ( var assembly in this._module.ReferencedAssemblySymbols )
        {
            yield return this._compilationModel.Factory.GetAssembly( assembly );
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this._module.ReferencedAssemblySymbols.Length;

    public IEnumerable<IAssembly> OfName( string name )
    {
        foreach ( var assembly in this._module.ReferencedAssemblySymbols )
        {
            if ( assembly.Identity.Name == name )
            {
                yield return this._compilationModel.Factory.GetAssembly( assembly );
            }
        }
    }
}