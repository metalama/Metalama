// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal class TypeMemberIdentifierGenerator
{
    private ConcurrentDictionary<INamedTypeSymbol, HashSet<string>> UsedNames { get; }

    public TypeMemberIdentifierGenerator( CompilationModel compilationModel )
    {
        this.UsedNames = new ConcurrentDictionary<INamedTypeSymbol, HashSet<string>>( compilationModel.CompilationContext.SymbolComparer );
    }

    private HashSet<string> InitializeType(INamedTypeSymbol typeSymbol)
    {
        var usedNames = new HashSet<string>( StringComparer.Ordinal );

        // Load names from existing type members.
        // TODO: This ignores lexical scope, i.e. should use SemanticModel.LookupSymbols, but for the current limited use it's ok.
        foreach ( var member in typeSymbol.GetMembers() )
        {
            usedNames.Add( member.Name );
        }

        return usedNames;
    }

    public string AllocateName( INamedTypeSymbol typeSymbol, string hint, bool alwaysUseSuffix)
    {
        var usedNames = this.UsedNames.GetOrAdd( typeSymbol, static ( typeSymbol, instance ) => instance.InitializeType( typeSymbol ), this );

        lock ( usedNames )
        {
            var name = alwaysUseSuffix ? $"{hint}_0" : hint;
            var index = alwaysUseSuffix ? 1 : 0;
            while ( usedNames.Contains( name ) )
            {
                name = $"{hint}_{++index}";
            }
            usedNames.Add( name );
            return name;
        }
    }
}