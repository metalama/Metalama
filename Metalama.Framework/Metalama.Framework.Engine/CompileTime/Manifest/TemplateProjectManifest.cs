// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
internal sealed class TemplateProjectManifest
{
    public static TemplateProjectManifest Empty { get; } =
        new( new TemplateSymbolManifest( "", ExecutionScope.RunTime, null, null, null ) );

    [JsonProperty]
    public TemplateSymbolManifest RootSymbol { get; }

    [JsonConstructor]
    public TemplateProjectManifest( TemplateSymbolManifest rootSymbol )
    {
        this.RootSymbol = rootSymbol;
    }

    public ExecutionScope GetExecutionScope( ISymbol symbol )
    {
        return this.GetNodeInfo( symbol ).ExecutionScope ?? ExecutionScope.RunTime;
    }

    public bool IsTemplate( ISymbol symbol ) => this.GetNodeInfo( symbol ).LastTemplateInfo != null;

    public ITemplateInfo? GetTemplateInfo( ISymbol symbol ) => this.GetNodeInfo( symbol ).LastTemplateInfo;

    private ( TemplateSymbolManifest? Node, ExecutionScope? ExecutionScope, ITemplateInfo? LastTemplateInfo ) GetNodeInfo( ISymbol symbol )
    {
        // This method should be called on the root node only.

        ExecutionScope? lastScopeValue = null;
        ITemplateInfo? lastTemplateInfo = null;

        var result = GetNodeRecursiveAndUpdateLastInfo( symbol );

        return (result, lastScopeValue, lastTemplateInfo);

        TemplateSymbolManifest? GetNodeRecursiveAndUpdateLastInfo( ISymbol s )
        {
            var node = GetNodeRecursive( s );

            if ( node != null )
            {
                if ( node.Scope != null )
                {
                    lastScopeValue = node.Scope;
                }

                if ( node.TemplateInfo != null && node.TemplateInfo.AttributeType != TemplateAttributeType.None )
                {
                    lastTemplateInfo = node;
                }
            }

            return node;
        }

        TemplateSymbolManifest? GetNodeRecursive( ISymbol s )
        {
            if ( s.Kind == SymbolKind.Namespace && s is INamespaceSymbol { IsGlobalNamespace: true } )
            {
                // This is the root node.
                return this.RootSymbol;
            }
            else
            {
                var parentNode = GetNodeRecursiveAndUpdateLastInfo( s.ContainingSymbol );

                if ( parentNode == null )
                {
                    return null;
                }

                if ( parentNode.Children?.TryGetValue( s.Name, out var children ) != true )
                {
                    return null;
                }
                else if ( s.Kind is SymbolKind.Namespace or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event or SymbolKind.Parameter
                         or SymbolKind.TypeParameter )
                {
                    // For members that are uniquely named, skip the comparison of id.

                    return children![0];
                }
                else
                {
                    var id = s.GetSerializableId().Id;

                    return children!.SingleOrDefault( x => x.Id == id );
                }
            }
        }
    }
}