// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
internal sealed class TemplateSymbolManifest : ITemplateInfo
{
    public string Id { get; }

    bool ITemplateInfo.IsAbstract => this.TemplateInfo?.IsAbstract ?? false;

    bool ITemplateInfo.HasNoBody => this.TemplateInfo?.HasNoBody ?? false;

    TemplateAttributeType ITemplateInfo.AttributeType => this.TemplateInfo?.AttributeType ?? TemplateAttributeType.None;

    bool ITemplateInfo.IsNone => this.TemplateInfo == null || this.TemplateInfo.AttributeType == TemplateAttributeType.None;

    public ExecutionScope? Scope { get; }

    public TemplateInfoManifest? TemplateInfo { get; }

    public RoslynApiVersion? UsedApiVersion { get; }

    /// <summary>
    /// Gets a dictionary of children, mapped by name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<TemplateSymbolManifest>>? Children { get; }

    [JsonConstructor]
    public TemplateSymbolManifest(
        string id,
        ExecutionScope? scope,
        TemplateInfoManifest? templateInfo,
        RoslynApiVersion? usedApiVersion,
        IReadOnlyDictionary<string, IReadOnlyList<TemplateSymbolManifest>>? children )
    {
        this.Id = id;
        this.Scope = scope;
        this.TemplateInfo = templateInfo;
        this.UsedApiVersion = usedApiVersion;
        this.Children = children;
    }

    public sealed class Builder
    {
        private readonly ISymbol _symbol;
        private Dictionary<string, List<Builder>>? _children;
        private TemplatingScope? _scope;
        private TemplateInfo? _templateInfo;
        private RoslynApiVersion? _usedApiVersion;

        public Builder( ISymbol symbol, TemplatingScope? scope = null, TemplateInfo? templateInfo = null, RoslynApiVersion? usedApiVersion = null )
        {
            this._symbol = symbol.Assert( s => s is not IMethodSymbol { MethodKind: MethodKind.LocalFunction } );
            this._scope = scope;
            this._templateInfo = templateInfo;
            this._usedApiVersion = usedApiVersion;
        }

        public Builder AddOrUpdateSymbol(
            ISymbol symbol,
            TemplatingScope? scope = null,
            TemplateInfo? templateInfo = null,
            RoslynApiVersion? usedApiVersion = null )
        {
            Builder parentBuilder;

            if ( symbol.ContainingSymbol == null )
            {
                throw new AssertionFailedException( "Containing symbol is null." );
            }
            else if ( this._symbol.Equals( symbol.ContainingSymbol ) )
            {
                parentBuilder = this;
            }
            else
            {
                parentBuilder = this.AddOrUpdateSymbol( symbol.ContainingSymbol );
            }

            parentBuilder._children ??= new Dictionary<string, List<Builder>>();

            if ( !parentBuilder._children.TryGetValue( symbol.Name, out var childrenList ) )
            {
                childrenList = new List<Builder>();
                parentBuilder._children[symbol.Name] = childrenList;
            }

            var builder = childrenList.SingleOrDefault( s => s._symbol.Equals( symbol ) );

            if ( builder != null )
            {
                // The node already exists.
                // Update the scope or template info if we have better information.

                if ( scope != null )
                {
                    builder._scope = scope;
                }

                if ( templateInfo != null )
                {
                    builder._templateInfo = templateInfo;
                }

                if ( usedApiVersion != null )
                {
                    builder._usedApiVersion = usedApiVersion;
                }
            }
            else
            {
                // Create a new node.
                builder = new Builder( symbol, scope, templateInfo, usedApiVersion );
                childrenList.Add( builder );
            }

            return builder;
        }

        public TemplateSymbolManifest Build()
            => new(
                this._symbol.GetSerializableId().Id,
                this._scope?.ToExecutionScope(),
                this._templateInfo == null ? null : new TemplateInfoManifest( this._templateInfo.AttributeType, this._templateInfo.IsAbstract, this._templateInfo.HasNoBody ),
                this._usedApiVersion,
                this._children?.ToDictionary( x => x.Key, x => (IReadOnlyList<TemplateSymbolManifest>) x.Value.SelectAsArray( b => b.Build() ) ) );
    }

    SerializableDeclarationId ITemplateInfo.Id => new( this.Id );

    public override string ToString() => $"{this.Id}: {this.Scope ?? ExecutionScope.Default}, {this.TemplateInfo?.AttributeType ?? TemplateAttributeType.None}";
}