// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.HierarchicalOptions;

public sealed partial class HierarchicalOptionsManager
{
    private sealed class OptionTypeNode
    {
        // Options are also evaluated after the linker, so we must use portable references
        private readonly ConcurrentDictionary<IRef<IDeclaration>, DeclarationNode> _optionsByDeclaration =
            new( RefEqualityComparer<IDeclaration>.Structural );

        private readonly IHierarchicalOptions _defaultOptions;
        private readonly IHierarchicalOptions _emptyOptions;
        private readonly HierarchicalOptionsManager _parent;
        private readonly EligibilityHelper _eligibilityHelper;
        private readonly Type _type;
        private readonly string _typeName;

        public HierarchicalOptionsAttribute Metadata { get; }

        public OptionTypeNode(
            HierarchicalOptionsManager parent,
            Type type,
            IUserDiagnosticSink diagnosticAdder,
            IHierarchicalOptions defaultOptions,
            IHierarchicalOptions emptyOptions,
            CompilationContext compilationContext )
        {
            this._parent = parent;
            this._type = type;
            this._defaultOptions = defaultOptions;
            this._emptyOptions = emptyOptions;
            this._typeName = type.FullName.AssertNotNull();
            var invoker = parent._serviceProvider.GetRequiredService<UserCodeInvoker>();
            var context = new UserCodeExecutionContext( parent._serviceProvider, UserCodeDescription.Create( "Instantiating {0}", type ), compilationContext );

            var prototype =
                invoker.Invoke( () => (IHierarchicalOptions) Activator.CreateInstance( type ).AssertNotNull(), context );

            this._eligibilityHelper = new EligibilityHelper( prototype, parent._serviceProvider, type );
            this._eligibilityHelper.PopulateRules( diagnosticAdder, compilationContext );
            this.Metadata = type.GetCustomAttributes<HierarchicalOptionsAttribute>().SingleOrDefault() ?? HierarchicalOptionsAttribute.Default;
        }

        public void AddOptionsInstance( HierarchicalOptionsInstance configurator, IDiagnosticAdder diagnosticAdder )
        {
            // ReSharper disable once InconsistentlySynchronizedField
            var declarationOptions = this.GetOrAddDeclarationNode( configurator.Declaration );

            // Check the eligibility of the options on the target declaration.
            var eligibility = this._eligibilityHelper.GetEligibility( configurator.Declaration, true );

            if ( eligibility == EligibleScenarios.None )
            {
                var justification = this._eligibilityHelper.GetIneligibilityJustification(
                    EligibleScenarios.Default | EligibleScenarios.Inheritance,
                    new DescribedObject<IDeclaration>( configurator.Declaration ) );

                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.OptionNotEligibleOnTarget.CreateRoslynDiagnostic(
                        configurator.Declaration.GetDiagnosticLocation(),
                        (this._type.Name, configurator.Declaration.DeclarationKind, configurator.Declaration, justification!) ) );

                return;
            }

            lock ( declarationOptions.Sync )
            {
                declarationOptions.DirectOptions =
                    MergeOptions( declarationOptions.DirectOptions, configurator.Options, ApplyChangesAxis.SameDeclaration, configurator.Declaration );
            }
        }

        public IHierarchicalOptions? GetOptions( IDeclaration declaration, bool ignoreNamespace = false )
        {
            var node = this.GetNodeAndComputeDirectOptions( declaration );

            if ( ignoreNamespace )
            {
                if ( node?.HasCachedMergedOptionsExcludingNamespace == true )
                {
                    // If we have a cached value, use it.
                    return node.CachedMergedOptionsExcludingNamespace;
                }
            }
            else
            {
                if ( node?.HasCachedMergedOptions == true )
                {
                    // If we have a cached value, use it.
                    return node.CachedMergedOptions;
                }
            }

            // Get options inherited from the base declaration.
            IHierarchicalOptions? baseDeclarationOptions;
            IHierarchicalOptions? containingDeclarationOptions;
            IHierarchicalOptions? namespaceOptions;

            switch ( declaration )
            {
                case INamedType namedType:
                    if ( this.Metadata.InheritedByDerivedTypes && namedType.BaseType?.Definition is { } baseType )
                    {
                        baseDeclarationOptions = this.GetOptions( baseType );
                    }
                    else
                    {
                        baseDeclarationOptions = null;
                    }

                    if ( this.Metadata.InheritedByNestedTypes && namedType.DeclaringType != null )
                    {
                        containingDeclarationOptions = this.GetOptions( namedType.DeclaringType, true );
                    }
                    else
                    {
                        containingDeclarationOptions = null;
                    }

                    namespaceOptions = !ignoreNamespace ? this.GetOptions( namedType.ContainingNamespace ) : null;

                    break;

                case IMember member:
                    if ( this.Metadata.InheritedByOverridingMembers && member.GetBase()?.Definition is { } baseDeclaration )
                    {
                        baseDeclarationOptions = this.GetOptions( baseDeclaration );
                    }
                    else
                    {
                        baseDeclarationOptions = null;
                    }

                    containingDeclarationOptions = this.Metadata.InheritedByMembers ? this.GetOptions( member.DeclaringType ) : null;

                    namespaceOptions = null;

                    break;

                case IAssembly { IsExternal: true }:
                    // Make sure not to go down to the compilation.
                    baseDeclarationOptions = null;

                    // An assembly that knows about an option will have the correct default set in its manifest.
                    // For assemblies that don't know about an option, we use options gotten by calling the default constructor.
                    containingDeclarationOptions = node?.DirectOptions == null ? this._emptyOptions : null;
                    namespaceOptions = null;

                    break;

                case ICompilation:
                    baseDeclarationOptions = null;
                    containingDeclarationOptions = this._defaultOptions;
                    namespaceOptions = null;

                    break;

                default:
                    baseDeclarationOptions = null;
                    namespaceOptions = null;
                    containingDeclarationOptions = this.GetOptions( declaration.ContainingDeclaration.AssertNotNull() );

                    break;
            }

            // Merge all options.
            var inheritedOptions = MergeOptions(
                baseDeclarationOptions,
                containingDeclarationOptions,
                ApplyChangesAxis.ContainingDeclaration,
                declaration );

            var inheritedOptionsWithNamespaceOptions = MergeOptions(
                namespaceOptions,
                inheritedOptions,
                ApplyChangesAxis.BaseDeclaration,
                declaration );

            var mergedOptions = MergeOptions(
                inheritedOptionsWithNamespaceOptions,
                node?.DirectOptions,
                ApplyChangesAxis.TargetDeclaration,
                declaration );

            // Cache the result.
            var shouldCache =
                declaration.DeclarationKind is DeclarationKind.Namespace or DeclarationKind.NamedType or DeclarationKind.Compilation ||
                (mergedOptions != null && (mergedOptions != baseDeclarationOptions || mergedOptions != containingDeclarationOptions));

            if ( shouldCache )
            {
                node ??= this.GetNodeAndComputeDirectOptions( declaration, true )!;

                if ( ignoreNamespace )
                {
                    node.CachedMergedOptionsExcludingNamespace = mergedOptions;
                }
                else
                {
                    node.CachedMergedOptions = mergedOptions;
                }
            }

            return mergedOptions;
        }

        private static IHierarchicalOptions? MergeOptions(
            IHierarchicalOptions? baseOptions,
            IHierarchicalOptions? overridingOptions,
            ApplyChangesAxis axis,
            IDeclaration declaration )
        {
            if ( baseOptions == null )
            {
                return overridingOptions;
            }
            else if ( overridingOptions == null )
            {
                return baseOptions;
            }
            else if ( ReferenceEquals( baseOptions, overridingOptions ) )
            {
                return overridingOptions;
            }
            else
            {
                return (IHierarchicalOptions) baseOptions.ApplyChanges( overridingOptions, new ApplyChangesContext( axis, declaration ) );
            }
        }

        private DeclarationNode GetOrAddDeclarationNode( IDeclaration declaration )
        {
            return this._optionsByDeclaration.GetOrAdd(
                declaration.ToRef(),
                static ( _, ctx ) =>
                {
                    var node = new DeclarationNode();

                    if ( !ctx.declaration.BelongsToCurrentProject )
                    {
                        if ( ctx.me._parent._externalOptionsProvider?.TryGetOptions( ctx.declaration, ctx.me._typeName, out var options ) == true )
                        {
                            node.DirectOptions = node.CachedMergedOptions = options;
                        }
                    }
                    else
                    {
                        // Note that in case of race we may wire a node that will be unused,
                        // but this should not affect the consistency of the data structure.
                        ctx.me.WireNodeToParents( ctx.declaration, node );
                    }

                    return node;
                },
                (me: this, declaration) );
        }

        private void WireNodeToParents( IDeclaration declaration, DeclarationNode node )
        {
            var baseDeclaration = (declaration as IMemberOrNamedType)?.GetBase()?.Definition;

            if ( baseDeclaration != null )
            {
                var baseNode = this.GetOrAddDeclarationNode( baseDeclaration );
                baseNode.AddChildNode( node );
            }

            var containingDeclaration = declaration.ContainingDeclaration;

            if ( containingDeclaration != null )
            {
                var containingNode = this.GetOrAddDeclarationNode( containingDeclaration );
                containingNode.AddChildNode( node );
            }
        }

        private DeclarationNode? GetNodeAndComputeDirectOptions(
            IDeclaration declaration,
            bool createNodeIfEmpty = false )
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if ( !this._optionsByDeclaration.TryGetValue( declaration.ToRef(), out var node ) )
            {
                if ( !declaration.BelongsToCurrentProject
                     && this._parent._externalOptionsProvider?.TryGetOptions( declaration, this._typeName, out _ ) == true )
                {
                    // If this is an external declaration and we have options, call GetOrAddDeclarationNode, which will set the options.
                    node = this.GetOrAddDeclarationNode( declaration );
                }
                else if ( createNodeIfEmpty )
                {
                    node = this.GetOrAddDeclarationNode( declaration );
                }
            }

            return node;
        }

        public IEnumerable<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>> GetInheritableOptions( ICompilation compilation, bool withSyntaxTree )
        {
            // We have to return the merged options of any node that has direct options. We don't return the whole cache because this cache may be incomplete.
            var optionsOnDeclarations = this._optionsByDeclaration
                .Where( x => x.Value.DirectOptions != null )
                .Select( x => (IDeclarationImpl) x.Key.GetTarget( compilation ) )
                .Where(
                    x => x is { DeclarationKind: DeclarationKind.Namespace } or
                        { CanBeInherited: true, BelongsToCurrentProject: true } )
                .Select(
                    x => new KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>(
                        new HierarchicalOptionsKey( this._typeName, x.ToSerializableId(), withSyntaxTree ? x.GetPrimarySyntaxTree()?.FilePath : null ),
                        this.GetOptions( x ).AssertNotNull() ) );

            // We have to return the assembly-level options even if no option has been specifically defined on the compilation because default
            // options may be project-dependency.
            var compilationOptions = this._defaultOptions;

            if ( this._optionsByDeclaration.TryGetValue( compilation.ToRef(), out var compilationNode )
                 && compilationNode.DirectOptions != null )
            {
                compilationOptions = MergeOptions( compilationOptions, compilationNode.DirectOptions, ApplyChangesAxis.SameDeclaration, compilation )
                    .AssertNotNull();
            }

            var defaultOptions = new KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>(
                new HierarchicalOptionsKey( this._typeName, compilation.ToSerializableId() ),
                compilationOptions );

            return optionsOnDeclarations.Concat( defaultOptions );
        }

        public void SetAspectOptions( IDeclaration declaration, IHierarchicalOptions options )
        {
            var node = this.GetNodeAndComputeDirectOptions( declaration, true ).AssertNotNull();

            lock ( node.Sync )
            {
                node.DirectOptions =
                    MergeOptions( node.DirectOptions, options, ApplyChangesAxis.Aspect, declaration );
            }
        }
    }
}