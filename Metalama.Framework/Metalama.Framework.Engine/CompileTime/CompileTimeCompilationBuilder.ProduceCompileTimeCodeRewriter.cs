// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed partial class CompileTimeCompilationBuilder
    {
        /// <summary>
        /// Rewrites a run-time syntax tree into a compile-time syntax tree. Calls <see cref="TemplateCompiler"/> on templates,
        /// and removes run-time-only sub trees.
        /// </summary>
        /// <remarks>Does not guarantee correctness of trivias. Preprocessor trivias need to be stripped afterwards. </remarks>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
        private sealed partial class ProduceCompileTimeCodeRewriter : SafeSyntaxRewriter
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
        {
            private static readonly SyntaxAnnotation _hasCompileTimeCodeAnnotation = new( "Metalama_HasCompileTimeCode" );
            private readonly Compilation _runTimeCompilation;

            private readonly Compilation _compileTimeCompilation;
            private readonly CompileTimeCompilationBuilder _parent;
            private readonly ImmutableArray<UsingDirectiveSyntax> _globalUsings;
            private readonly IReadOnlyDictionary<INamedTypeSymbol, SerializableTypeInfo> _serializableTypes;
            private readonly IReadOnlyDictionary<ISymbol, SerializableTypeInfo> _serializableFieldsAndProperties;
            private readonly IDiagnosticAdder _diagnosticAdder;
            private readonly TemplateCompiler _templateCompiler;
            private readonly CancellationToken _cancellationToken;
            private readonly SyntaxGenerationContext _syntaxGenerationContext;
            private readonly CompilationContext _runtimeCompilationContext;
            private readonly NameSyntax _originalNameTypeSyntax;
            private readonly NameSyntax _originalPathTypeSyntax;
            private readonly ITypeSymbol _fabricType;
            private readonly ITypeSymbol _typeFabricType;
            private readonly ISerializerGenerator _serializerGenerator;
            private readonly TypeOfRewriter _typeOfRewriter;
            private readonly RewriterHelper _helper;
            private readonly TemplateProjectManifestBuilder _compileTimeManifestBuilder;
            private readonly SafeSymbolComparer _symbolEqualityComparer;

            private Context _currentContext;
            private HashSet<string>? _currentTypeTemplateNames;
            private string? _currentTypeName;
            private IReadOnlyDictionary<ISymbol, HashSet<ISymbol>>? _currentTypeImplicitInterfaceImplementations;

            public bool Success { get; private set; } = true;

            public bool FoundCompileTimeCode { get; private set; }

            public bool ReferencesMetalamaSdk { get; private set; }

            private SemanticModelProvider RunTimeSemanticModelProvider => this._helper.SemanticModelProvider;

            public ProduceCompileTimeCodeRewriter(
                ProjectServiceProvider serviceProvider,
                CompileTimeCompilationBuilder parent,
                ClassifyingCompilationContext compilationContext,
                CompilationContext compileTimeCompilationContext,
                IReadOnlyList<SerializableTypeInfo> serializableTypes,
                ImmutableArray<UsingDirectiveSyntax> globalUsings,
                IDiagnosticAdder diagnosticAdder,
                TemplateCompiler templateCompiler,
                IEnumerable<CompileTimeProject> referencedProjects,
                TemplateProjectManifestBuilder templateManifestBuilder,
                CancellationToken cancellationToken )
            {
                this._compileTimeManifestBuilder = templateManifestBuilder;
                this._helper = new RewriterHelper( compilationContext, ReplaceDynamicToObjectRewriter.Rewrite );
                this._runTimeCompilation = compilationContext.SourceCompilation;
                this._compileTimeCompilation = compileTimeCompilationContext.Compilation;
                this._parent = parent;
                this._globalUsings = globalUsings;
                this._diagnosticAdder = diagnosticAdder;
                this._templateCompiler = templateCompiler;
                this._cancellationToken = cancellationToken;
                this._currentContext = new Context( TemplatingScope.RunTimeOrCompileTime, null, null, 0, this );

                this._symbolEqualityComparer = compilationContext.CompilationContext.SymbolComparer;

                this._serializableTypes =
                    serializableTypes.ToDictionary<SerializableTypeInfo, INamedTypeSymbol, SerializableTypeInfo>(
                        x => x.Type,
                        x => x,
                        this._symbolEqualityComparer );

                this._serializableFieldsAndProperties =
                    serializableTypes.SelectMany( x => x.SerializedMembers.SelectAsReadOnlyList( y => (Member: y, Type: x) ) )
                        .ToDictionary( x => x.Member, x => x.Type, this._symbolEqualityComparer );

                this._syntaxGenerationContext = compileTimeCompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );
                this._runtimeCompilationContext = compilationContext.CompilationContext;

                // TODO: This should be probably injected as a service, but we are creating the generation context here.
                this._serializerGenerator = new SerializerGenerator(
                    serviceProvider,
                    diagnosticAdder,
                    compilationContext.CompilationContext,
                    compileTimeCompilationContext,
                    this._syntaxGenerationContext,
                    referencedProjects );

                this._typeOfRewriter = new TypeOfRewriter( this._syntaxGenerationContext );

                this._originalNameTypeSyntax = (NameSyntax)
                    this._syntaxGenerationContext.SyntaxGenerator.TypeSyntax(
                        this._syntaxGenerationContext.ReflectionMapper.GetTypeSymbol( typeof(OriginalIdAttribute) ) );

                this._originalPathTypeSyntax = (NameSyntax)
                    this._syntaxGenerationContext.SyntaxGenerator.TypeSyntax(
                        this._syntaxGenerationContext.ReflectionMapper.GetTypeSymbol( typeof(OriginalPathAttribute) ) );

                this._fabricType = compilationContext.ReflectionMapper.GetTypeSymbol( typeof(Fabric) );
                this._typeFabricType = compilationContext.ReflectionMapper.GetTypeSymbol( typeof(TypeFabric) );
            }

            private ISymbolClassifier SymbolClassifier => this._helper.SymbolClassifier;

            public override SyntaxNode? VisitAttributeList( AttributeListSyntax node )
            {
                if ( node.Parent is CompilationUnitSyntax )
                {
                    return null;
                }

                var filteredAttributes = new List<AttributeSyntax>( node.Attributes.Count );

                foreach ( var attribute in node.Attributes )
                {
                    var semanticModel = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree );
                    var attributeSymbol = semanticModel.GetSymbolInfo( attribute.Name ).Symbol;

                    if ( attributeSymbol != null )
                    {
                        if ( this.SymbolClassifier.GetTemplatingScope( attributeSymbol ) == TemplatingScope.RunTimeOnly )
                        {
                            var attributeTypeSymbol = attributeSymbol.GetClosestContainingType();

                            if ( attributeTypeSymbol?.GetFullName() == "System.Runtime.CompilerServices.InlineArrayAttribute" )
                            {
                                var containingDeclaration = node.Parent == null ? null : semanticModel.GetDeclaredSymbol( node.Parent );

                                this._diagnosticAdder.Report(
                                    TemplatingDiagnosticDescriptors.AttributeNotAllowedOnCompileTimeCode.CreateRoslynDiagnostic(
                                        attribute.GetDiagnosticLocation(),
                                        (attributeTypeSymbol, containingDeclaration) ) );

                                this.Success = false;
                            }

                            continue;
                        }
                    }

                    var item = (AttributeSyntax?) this.Visit( attribute );

                    if ( item != null )
                    {
                        filteredAttributes.Add( item );
                    }
                }

                if ( filteredAttributes.Count == 0 )
                {
                    return null;
                }
                else
                {
                    return node.WithAttributes( SeparatedList( filteredAttributes ) );
                }
            }

            private SyntaxList<AttributeListSyntax> VisitAttributeLists( SyntaxList<AttributeListSyntax> attributeLists )
                => List( attributeLists.SelectAsReadOnlyList( l => (AttributeListSyntax?) this.VisitAttributeList( l ) ).WhereNotNull() );

            public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node ).SingleOrDefault();

            public override SyntaxNode? VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node ).SingleOrDefault();

            public override SyntaxNode? VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitTypeDeclaration( node ).SingleOrDefault();

            public override SyntaxNode? VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node ).SingleOrDefault();

            public override SyntaxNode? VisitEnumDeclaration( EnumDeclarationSyntax node )
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node ).AssertNotNull();
                var scope = this._helper.SymbolClassifier.GetTemplatingScope( symbol );

                if ( scope == TemplatingScope.RunTimeOnly )
                {
                    // Make sure to visit the node so we process the preprocessor directives.
                    base.VisitEnumDeclaration( node );

                    return null;
                }
                else
                {
                    this.FoundCompileTimeCode = true;

                    return base.VisitEnumDeclaration( node )!.WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation );
                }
            }

            public override SyntaxNode? VisitDelegateDeclaration( DelegateDeclarationSyntax node )
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node ).AssertNotNull();
                var scope = this.SymbolClassifier.GetTemplatingScope( symbol );

                if ( scope == TemplatingScope.RunTimeOnly )
                {
                    // Make sure to visit the node so we process the preprocessor directives.
                    base.VisitDelegateDeclaration( node );

                    return null;
                }
                else
                {
                    return base.VisitDelegateDeclaration( node )!.WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation );
                }
            }

            private void PopulateNestedCompileTimeTypes( TypeDeclarationSyntax node, List<MemberDeclarationSyntax> list, string namePrefix, int nestingLevel )
            {
                // Compute the new name of the relocated children.
                namePrefix += node.Identifier.Text;

                if ( node.TypeParameterList is { Parameters.Count: > 0 } )
                {
                    // This does not guarantee the absence of conflict.
                    namePrefix += "X" + node.TypeParameterList.Parameters.Count;
                }

                namePrefix += "_";

                foreach ( var child in node.Members )
                {
                    var childSymbol = this.RunTimeSemanticModelProvider.GetSemanticModel( child.SyntaxTree ).GetDeclaredSymbol( child )
                        as ITypeSymbol;

                    switch ( child )
                    {
                        case ClassDeclarationSyntax childType:
                            {
                                Invariant.Assert( childSymbol != null );

                                var childScope = this.SymbolClassifier.GetTemplatingScope( childSymbol ).GetExpressionExecutionScope();

                                switch ( childScope )
                                {
                                    case TemplatingScope.CompileTimeOnly:
                                        {
                                            // We have a build-time type nested in a run-time type. We have to un-nest it.

                                            // Check that the visibility is private.
                                            if ( childSymbol.DeclaredAccessibility != Accessibility.Private )
                                            {
                                                this._diagnosticAdder.Report(
                                                    TemplatingDiagnosticDescriptors.NestedCompileTypesMustBePrivate.CreateRoslynDiagnostic(
                                                        childType.Identifier.GetLocation(),
                                                        childSymbol ) );
                                            }

                                            // Check that it inherits TypeFabric.
                                            if ( !this._runTimeCompilation.HasImplicitConversion( childSymbol, this._typeFabricType ) )
                                            {
                                                this._diagnosticAdder.Report(
                                                    TemplatingDiagnosticDescriptors.RunTimeTypesCannotHaveCompileTimeTypesExceptTypeFabrics
                                                        .CreateRoslynDiagnostic(
                                                            childSymbol.GetDiagnosticLocation(),
                                                            (childSymbol, typeof(TypeFabric)) ) );

                                                this.Success = false;
                                            }

                                            // Create the [OriginalId] attribute.
                                            var originalId = DocumentationCommentId.CreateDeclarationId( childSymbol );

                                            var originalNameAttribute = Attribute( this._originalNameTypeSyntax )
                                                .WithArgumentList(
                                                    AttributeArgumentList(
                                                        SingletonSeparatedList( AttributeArgument( SyntaxFactoryEx.LiteralExpression( originalId ) ) ) ) );

                                            // Transform the type.
                                            TypeDeclarationSyntax transformedChild;
                                            var newName = namePrefix + "" + childType.Identifier.Text;

                                            using ( this.WithUnnestedType( (INamedTypeSymbol) childSymbol, newName, nestingLevel ) )
                                            {
                                                transformedChild = (TypeDeclarationSyntax) this.Visit( childType )!;
                                            }

                                            // Rename the type and add [OriginalId].

                                            transformedChild = transformedChild
                                                .WithIdentifier( Identifier( newName ) )
                                                .WithModifiers( TokenList( Token( SyntaxKind.InternalKeyword ).WithTrailingTrivia( ElasticSpace ) ) )
                                                .WithAttributeLists(
                                                    transformedChild.AttributeLists.Add( AttributeList( SingletonSeparatedList( originalNameAttribute ) ) ) );

                                            list.Add( transformedChild );

                                            break;
                                        }

                                    case TemplatingScope.RunTimeOnly:
                                        // We have a run-time child type, and it must be further checked for un-nesting.

                                        this.PopulateNestedCompileTimeTypes( childType, list, namePrefix, nestingLevel + 1 );

                                        break;

                                    default:
                                        this._diagnosticAdder.Report(
                                            TemplatingDiagnosticDescriptors.NeutralTypesForbiddenInNestedRunTimeTypes.CreateRoslynDiagnostic(
                                                childType.Identifier.GetLocation(),
                                                childSymbol ) );

                                        break;
                                }

                                break;
                            }

                        case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                            Invariant.Assert( childSymbol != null );

                            if ( this.SymbolClassifier.GetTemplatingScope( childSymbol ).GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly )
                            {
                                this._diagnosticAdder.Report(
                                    TemplatingDiagnosticDescriptors.RunTimeTypesCannotHaveCompileTimeTypesExceptTypeFabrics.CreateRoslynDiagnostic(
                                        childSymbol.GetDiagnosticLocation(),
                                        (childSymbol, typeof(TypeFabric)) ) );

                                this.Success = false;
                            }

                            break;

                        // ReSharper disable once RedundantEmptySwitchSection
                        default:
                            // Non-type members of a run-time type are always run-time too and should not be copied to the compile-time assembly.
                            break;
                    }
                }
            }

            private void AddToManifestIfNecessary(
                ISymbol symbol,
                TemplateInfo? templateInfo,
                TemplatingScope? scope = default,
                params IMethodSymbol?[] accessors )
            {
                scope ??= this.SymbolClassifier.GetTemplatingScope( symbol );

                if ( templateInfo is { IsNone: false } || scope != TemplatingScope.RunTimeOnly )
                {
                    var executionScope = scope.Value.GetExpressionExecutionScope();

                    this._compileTimeManifestBuilder.AddOrUpdateSymbol( symbol, executionScope, templateInfo );

                    // For properties and events, we also update the symbols of accessors. It makes the manifest longer, but reading the manifest
                    // is then faster.
                    foreach ( var accessor in accessors )
                    {
                        if ( accessor != null )
                        {
                            this._compileTimeManifestBuilder.AddOrUpdateSymbol( accessor, executionScope, templateInfo );

                            // Mark all accessor parameters as run-time.
                            foreach ( var parameter in accessor.Parameters )
                            {
                                this._compileTimeManifestBuilder.AddOrUpdateSymbol( parameter, TemplatingScope.RunTimeOnly );
                            }
                        }
                    }
                }
            }

            private void AddToManifest( ISymbol symbol, RoslynApiVersion? usedApiVersion )
            {
                this._compileTimeManifestBuilder.AddOrUpdateSymbol( symbol, usedApiVersion: usedApiVersion );
            }

            private IEnumerable<MemberDeclarationSyntax> VisitTypeDeclaration( TypeDeclarationSyntax node )
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node ).AssertNotNull();

                // Eliminate system types.
                if ( SystemTypeDetector.IsSystemType( symbol ) )
                {
                    return Array.Empty<MemberDeclarationSyntax>();
                }

                var scope = this.SymbolClassifier.GetTemplatingScope( symbol );

                if ( scope == TemplatingScope.RunTimeOnly )
                {
                    // If the type contains compile-time nested types, we have to un-nest them.
                    var compileTimeMembers = new List<MemberDeclarationSyntax>();
                    this.PopulateNestedCompileTimeTypes( node, compileTimeMembers, "", 1 );

                    return compileTimeMembers;
                }
                else
                {
                    this.AddToManifestIfNecessary( symbol, null );

                    var transformedNode = this.TransformCompileTimeType( node, symbol, scope );

                    return [transformedNode];
                }
            }

            private TypeDeclarationSyntax TransformCompileTimeType( TypeDeclarationSyntax node, INamedTypeSymbol symbol, TemplatingScope scope )
            {
                this.FoundCompileTimeCode = true;

                this._currentTypeTemplateNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
                this._currentTypeName = symbol.Name;
                this._currentTypeImplicitInterfaceImplementations = this.GetImplicitlyImplementedInterfaceMembers( symbol );

                // Check the diagnostics in this type.
                // At compile time, any diagnostic in compile-time code must be reported because it will be removed from the final compilation.
                // In case of templates, the code will be transformed, and understanding diagnostics in the transformed code is highly cumbersome.

                var typeHasError = false;

                var compileTimeDiagnostics = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree )
                    .GetDiagnostics( node.Span, this._cancellationToken );

                foreach ( var diagnostic in compileTimeDiagnostics )
                {
                    this._diagnosticAdder.Report( diagnostic );

                    if ( diagnostic.Severity == DiagnosticSeverity.Error && !diagnostic.IsWarningAsError )
                    {
                        typeHasError = true;
                    }
                }

                if ( typeHasError )
                {
                    if ( this._parent._logger.Warning != null )
                    {
                        var diagnostics = compileTimeDiagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToReadOnlyList();

                        this._parent._logger.Warning.Log(
                            $"Compiling the compile-time project failed because the source code contains {diagnostics.Count} C# error(s):" );

                        foreach ( var error in diagnostics )
                        {
                            this._parent._logger.Warning.Log( error.ToString() );
                        }

                        // We report an error because if some some reasons (because of a bug) these errors were _not_ reported to the user,
                        // we would silently fail the compilation, and this would be very difficult to diagnose.
                        this._diagnosticAdder.Report( GeneralDiagnosticDescriptors.ErrorsInSourceCode.CreateRoslynDiagnostic( null, default ) );
                    }

                    this.Success = false;

                    return node;
                }

                // Add type members.

                var members = new List<MemberDeclarationSyntax>();

                using ( this.WithScope( scope ) )
                {
                    foreach ( var member in node.Members )
                    {
                        switch ( member )
                        {
                            case MethodDeclarationSyntax method:
                                members.AddRange( this.TransformMethodDeclaration( method ).AssertNoneNull() );

                                break;

                            case IndexerDeclarationSyntax:
                                throw new NotImplementedException( "Indexers are not implemented." );

                            // members.AddRange( this.VisitBasePropertyDeclaration( indexer ).AssertNoneNull() );

                            case PropertyDeclarationSyntax property:
                                members.AddRange( this.TransformPropertyDeclaration( property ).AssertNoneNull() );

                                break;

                            case EventDeclarationSyntax @event:
                                members.AddRange( this.TransformEventDeclaration( @event ).AssertNoneNull() );

                                break;

                            case FieldDeclarationSyntax field:
                                members.AddRange( this.TransformFieldDeclaration( field ).AssertNoneNull() );

                                break;

                            case EventFieldDeclarationSyntax eventField:
                                members.AddRange( this.TransformEventFieldDeclaration( eventField ).AssertNoneNull() );

                                break;

                            default:
                                members.Add( (MemberDeclarationSyntax) this.Visit( member ).AssertNotNull() );

                                break;
                        }
                    }
                }

                // Add non-implemented members of IAspect, IEligible, IHierarchicalOptions.
                var syntaxGenerator = this._syntaxGenerationContext.SyntaxGenerator;
                var allImplementedInterfaces = symbol.SelectManyRecursiveDistinct( i => i.Interfaces );

                foreach ( var implementedInterface in allImplementedInterfaces )
                {
                    if ( implementedInterface.Name is nameof(IAspect) or nameof(IEligible<IDeclaration>) or nameof(IHierarchicalOptions) )
                    {
                        foreach ( var member in implementedInterface.GetMembers() )
                        {
                            if ( member is not IMethodSymbol method )
                            {
                                // IAspect and IEligible have only methods.
                                throw new AssertionFailedException( $"Unexpected member '{member}'." );
                            }

                            var memberImplementation = (IMethodSymbol?) symbol.FindImplementationForInterfaceMember( member );

                            if ( memberImplementation == null || memberImplementation.ContainingType.TypeKind == TypeKind.Interface )
                            {
                                var newMethod = MethodDeclaration(
                                        default,
                                        default,
                                        syntaxGenerator.TypeSyntax( method.ReturnType ),
                                        ExplicitInterfaceSpecifier( (NameSyntax) syntaxGenerator.TypeSyntax( implementedInterface ) ),
                                        Identifier( method.Name ),
                                        default,
                                        ParameterList(
                                            SeparatedList(
                                                method.Parameters.Select(
                                                    p => Parameter(
                                                        default,
                                                        default,
                                                        syntaxGenerator.TypeSyntax( p.Type ),
                                                        Identifier( p.Name ),
                                                        default ) ) ) ),
                                        default,
                                        method.ReturnType.SpecialType == SpecialType.System_Void
                                            ? this._syntaxGenerationContext.SyntaxGenerator.FormattedBlock()
                                            : null,
                                        method.ReturnType.SpecialType == SpecialType.System_Void ? null : ArrowExpressionClause( SyntaxFactoryEx.Default ),
                                        method.ReturnType.SpecialType == SpecialType.System_Void ? default : Token( SyntaxKind.SemicolonToken ) )
                                    .NormalizeWhitespace( eol: this._syntaxGenerationContext.EndOfLine );

                                members.Add( newMethod );
                            }
                        }
                    }
                }

                // Add serialization logic if the type is serializable and does not have existing serializer and this is the primary declaration.
                if ( this._serializableTypes.TryGetValue( symbol, out var serializableType )
                     && symbol.GetPrimaryDeclarationSyntax() == node )
                {
                    if ( !SerializerGeneratorHelper.TryGetSerializer(
                            this._runtimeCompilationContext,
                            symbol,
                            out var existingSerializer,
                            out var ambiguous ) && ambiguous )
                    {
                        throw new AssertionFailedException( $"Ambiguous serializer for {symbol}. This should have been caught by TemplatingCodeValidator." );
                    }
                    else if ( existingSerializer == null )
                    {
                        var serializedTypeName = this.CreateTypeSyntax( serializableType.Type ).AssertCast<NameSyntax>();
                        var constructorName = GetConstructorNameToken( serializedTypeName );

                        if ( !serializableType.Type.IsValueType
                             && !serializableType.Type.GetMembers()
                                 .Any( m => m is IMethodSymbol { MethodKind: MethodKind.Constructor } method && method.GetPrimarySyntaxReference() != null ) )
                        {
                            // There is no defined constructor, so we need to explicitly add parameterless constructor (only for reference types).
                            members.Add(
                                ConstructorDeclaration(
                                        List<AttributeListSyntax>(),
                                        TokenList( Token( SyntaxKind.PublicKeyword ).WithTrailingTrivia( ElasticSpace ) ),
                                        constructorName,
                                        ParameterList(),
                                        null,
                                        this._syntaxGenerationContext.SyntaxGenerator.FormattedBlock(),
                                        null )
                                    .NormalizeWhitespace( eol: this._syntaxGenerationContext.EndOfLine ) );
                        }

                        var deserializingConstructor = this._serializerGenerator.CreateDeserializingConstructor( serializableType, constructorName );
                        var serializerType = this._serializerGenerator.CreateSerializerType( serializableType, serializedTypeName );

                        if ( deserializingConstructor != null && serializerType != null )
                        {
                            members.Add( deserializingConstructor.NormalizeWhitespace( eol: this._syntaxGenerationContext.EndOfLine ) );
                            members.Add( serializerType.NormalizeWhitespace( eol: this._syntaxGenerationContext.EndOfLine ) );
                        }
                        else
                        {
                            // Above method calls return null when they fail.
                            this.Success = false;
                        }
                    }
                }

                var transformedNode = node.WithMembers( List( members ) )
                    .WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation )
                    .WithAttributeLists( this.VisitAttributeLists( node.AttributeLists ) );

                // If the type is a fabric, add the OriginalPath attribute.
                if ( this._runTimeCompilation.HasImplicitConversion( symbol, this._fabricType ) )
                {
                    var originalPathAttribute = Attribute( this._originalPathTypeSyntax )
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList( AttributeArgument( SyntaxFactoryEx.LiteralExpression( node.SyntaxTree.FilePath ) ) ) ) );

                    transformedNode = transformedNode
                        .WithAttributeLists( transformedNode.AttributeLists.Add( AttributeList( SingletonSeparatedList( originalPathAttribute ) ) ) );
                }

                return transformedNode;
            }

            private IReadOnlyDictionary<ISymbol, HashSet<ISymbol>> GetImplicitlyImplementedInterfaceMembers( INamedTypeSymbol type )
            {
                if ( type is not { TypeKind: TypeKind.Class or TypeKind.Struct } )
                {
                    return ImmutableDictionary<ISymbol, HashSet<ISymbol>>.Empty;
                }

                var implicitInterfaceMembers = new Dictionary<ISymbol, HashSet<ISymbol>>();

                foreach ( var interfaceType in type.AllInterfaces )
                {
                    foreach ( var interfaceMember in interfaceType.GetMembers() )
                    {
                        var interfaceMemberImplementation = type.FindImplementationForInterfaceMember( interfaceMember );

                        if ( interfaceMemberImplementation == null )
                        {
                            continue;
                        }

                        if ( this._symbolEqualityComparer.Equals( interfaceMemberImplementation.ContainingType, type )
                             && !interfaceMemberImplementation.IsExplicitInterfaceMemberImplementation() )
                        {
                            // The interface member is implemented in the current type.
                            if ( !implicitInterfaceMembers.TryGetValue( interfaceMemberImplementation, out var implementedInterfaceMembers ) )
                            {
                                implicitInterfaceMembers[interfaceMemberImplementation] =
                                    implementedInterfaceMembers = new HashSet<ISymbol>( this._symbolEqualityComparer );
                            }

                            implementedInterfaceMembers.Add( interfaceMember );
                        }
                    }
                }

                return implicitInterfaceMembers;
            }

            private bool CheckTemplateName( ISymbol symbol )
            {
                if ( this._currentTypeTemplateNames!.Add( symbol.Name ) )
                {
                    // It's the first time we're seeing this name.
                    return true;
                }
                else
                {
                    this.Success = false;

                    this._diagnosticAdder.Report(
                        GeneralDiagnosticDescriptors.TemplateWithSameNameAlreadyDefined.CreateRoslynDiagnostic(
                            symbol.GetDiagnosticLocation(),
                            (symbol.Name, this._currentTypeName!) ) );

                    return false;
                }
            }

            private bool ShouldExcludeMember( ISymbol symbol )
            {
                if ( this.SymbolClassifier.GetTemplatingScope( symbol ) is TemplatingScope.RunTimeOnly or TemplatingScope.CompileTimeOnlyReturningRuntimeOnly
                     && this.SymbolClassifier.GetTemplateInfo( symbol ).IsNone )
                {
                    if ( symbol.DeclaredAccessibility is Accessibility.Internal or Accessibility.Public or Accessibility.ProtectedOrInternal &&
                         symbol is not (IFieldSymbol or IPropertySymbol)
                         && this.SymbolClassifier.GetTemplatingScope( symbol.ContainingType ) == TemplatingScope.RunTimeOrCompileTime )
                    {
                        // TODO
                    }

                    return true;
                }
                else 
                {
                    return false;
                }
            }

            private IEnumerable<MethodDeclarationSyntax> TransformMethodDeclaration( MethodDeclarationSyntax node )
            {
                var methodSymbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node );

                if ( methodSymbol == null || this.ShouldExcludeMember( methodSymbol ) )
                {
                    yield break;
                }

                var templateInfo = this.SymbolClassifier.GetTemplateInfo( methodSymbol );

                this.AddToManifestIfNecessary( methodSymbol, templateInfo );

                if ( templateInfo.IsNone )
                {
                    yield return (MethodDeclarationSyntax) this.VisitMethodDeclaration( node ).AssertNotNull();

                    yield break;
                }

                if ( templateInfo.HasNoBody )
                {
                    yield break;
                }

                // Templates of [Template] kind must be unique by name.
                if ( templateInfo.AttributeType == TemplateAttributeType.Template && !this.CheckTemplateName( methodSymbol ) )
                {
                    yield break;
                }

                var success =
                    this._templateCompiler.TryCompile(
                        TemplateNameHelper.GetCompiledTemplateName( methodSymbol ),
                        this._compileTimeCompilation,
                        node,
                        TemplateCompilerSemantics.Default,
                        this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                        this._diagnosticAdder,
                        this._cancellationToken,
                        out _,
                        out var transformedNode,
                        out var usedApiVersion );

                this.AddToManifest( methodSymbol, usedApiVersion );

                if ( success )
                {
                    if ( methodSymbol.IsAbstract )
                    {
                        yield return node;
                    }
                    else if ( (methodSymbol.IsOverride && methodSymbol.OverriddenMethod!.IsAbstract) || methodSymbol.IsExtern )
                    {
                        yield return this._helper.WithThrowNotSupportedExceptionBody(
                            node,
                            "Template code cannot be directly executed.",
                            this._syntaxGenerationContext );
                    }
                    else
                    {
                        // The method can be deleted, i.e. it does not need to be inserted back in the member list.
                    }

                    yield return (MethodDeclarationSyntax) transformedNode.AssertNotNull();
                }
                else
                {
                    this.Success = false;
                }
            }

            private IEnumerable<MemberDeclarationSyntax> TransformPropertyDeclaration( BasePropertyDeclarationSyntax node )
            {
                var propertySymbol = (IPropertySymbol?) this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node );

                if ( propertySymbol == null || this.ShouldExcludeMember( propertySymbol ) )
                {
                    yield break;
                }

                var templateInfo = this.SymbolClassifier.GetTemplateInfo( propertySymbol );

                this.AddToManifestIfNecessary( propertySymbol, templateInfo, null, propertySymbol.GetMethod, propertySymbol.SetMethod );

                if ( templateInfo.HasNoBody )
                {
                    yield break;
                }

                var propertyIsTemplate = !templateInfo.IsNone;
                var propertyOrAccessorsAreTemplate = propertyIsTemplate;

                var success = true;
                SyntaxNode? transformedGetDeclaration = null;
                SyntaxNode? transformedSetDeclaration = null;

                // Compile accessors into templates.
                if ( !propertySymbol.IsAbstract )
                {
                    if ( node.AccessorList != null )
                    {
                        var templateAccessorCount = 0;

                        var getAccessor = node.AccessorList.Accessors.SingleOrDefault( a => a.Kind() == SyntaxKind.GetAccessorDeclaration );

                        var getterIsTemplate = getAccessor != null
                                               && (propertyIsTemplate || !this.SymbolClassifier.GetTemplateInfo( propertySymbol.GetMethod! ).IsNone);

                        var setAccessor = node.AccessorList.Accessors.SingleOrDefault(
                            a => a.Kind() == SyntaxKind.SetAccessorDeclaration || a.Kind() == SyntaxKind.InitAccessorDeclaration );

                        var setterIsTemplate = setAccessor != null
                                               && (propertyIsTemplate || !this.SymbolClassifier.GetTemplateInfo( propertySymbol.SetMethod! ).IsNone);

                        // Auto properties don't have bodies and so we don't need templates.

                        RoslynApiVersion? maxApiVersion = null;

                        if ( getterIsTemplate && (getAccessor!.Body != null || getAccessor.ExpressionBody != null) )
                        {
                            if ( success )
                            {
                                success =
                                    this._templateCompiler.TryCompile(
                                        TemplateNameHelper.GetCompiledTemplateName( propertySymbol.GetMethod.AssertNotNull() ),
                                        this._compileTimeCompilation,
                                        getAccessor,
                                        TemplateCompilerSemantics.Default,
                                        this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                        this._diagnosticAdder,
                                        this._cancellationToken,
                                        out _,
                                        out transformedGetDeclaration,
                                        out var getterApiVersion );

                                maxApiVersion ??= getterApiVersion;
                            }

                            templateAccessorCount++;
                        }

                        if ( setterIsTemplate && (setAccessor!.Body != null || setAccessor.ExpressionBody != null) )
                        {
                            if ( success )
                            {
                                success =
                                    this._templateCompiler.TryCompile(
                                        TemplateNameHelper.GetCompiledTemplateName( propertySymbol.SetMethod.AssertNotNull() ),
                                        this._compileTimeCompilation,
                                        setAccessor,
                                        TemplateCompilerSemantics.Default,
                                        this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                        this._diagnosticAdder,
                                        this._cancellationToken,
                                        out _,
                                        out transformedSetDeclaration,
                                        out var setterApiVersion );

                                if ( maxApiVersion == null || setterApiVersion > maxApiVersion )
                                {
                                    maxApiVersion = setterApiVersion;
                                }
                            }

                            templateAccessorCount++;
                        }

                        if ( propertyIsTemplate && node is PropertyDeclarationSyntax { Initializer: not null } )
                        {
                            if ( success )
                            {
                                success =
                                    this._templateCompiler.TryCompile(
                                        TemplateNameHelper.GetCompiledTemplateName( propertySymbol ),
                                        this._compileTimeCompilation,
                                        node,
                                        TemplateCompilerSemantics.Initializer,
                                        this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                        this._diagnosticAdder,
                                        this._cancellationToken,
                                        out _,
                                        out transformedGetDeclaration,
                                        out var initializerApiVersion );

                                if ( maxApiVersion == null || initializerApiVersion > maxApiVersion )
                                {
                                    maxApiVersion = initializerApiVersion;
                                }
                            }
                        }

                        this.AddToManifest( propertySymbol, maxApiVersion );

                        if ( templateAccessorCount > 0 )
                        {
                            propertyOrAccessorsAreTemplate = true;

                            if ( templateAccessorCount != node.AccessorList.Accessors.Count )
                            {
                                throw new AssertionFailedException( "When one accessor is a template, the other must also be a template." );
                            }
                        }
                    }
                    else if ( propertyIsTemplate && node is PropertyDeclarationSyntax { ExpressionBody: not null } propertyNode )
                    {
                        // Expression bodied property.
                        // TODO: Does this preserve trivia in expression body?
                        if ( success )
                        {
                            success =
                                this._templateCompiler.TryCompile(
                                    TemplateNameHelper.GetCompiledTemplateName( propertySymbol.GetMethod.AssertNotNull() ),
                                    this._compileTimeCompilation,
                                    propertyNode,
                                    TemplateCompilerSemantics.Default,
                                    this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                    this._diagnosticAdder,
                                    this._cancellationToken,
                                    out _,
                                    out transformedGetDeclaration,
                                    out var getterApiVersion );

                            this.AddToManifest( propertySymbol, getterApiVersion );
                        }
                    }
                }

                if ( success )
                {
                    if ( !propertyOrAccessorsAreTemplate )
                    {
                        var suppressReadOnly = false;

                        if ( this._serializableFieldsAndProperties.TryGetValue( propertySymbol, out var serializableTypeInfo ) )
                        {
                            suppressReadOnly = this._serializerGenerator.ShouldSuppressReadOnly( serializableTypeInfo, propertySymbol );
                        }

                        var rewritten = (BasePropertyDeclarationSyntax) this.Visit( node ).AssertNotNull();

                        if ( suppressReadOnly && rewritten is PropertyDeclarationSyntax rewrittenProperty )
                        {
                            // If the property needs to have set accessor because of serialization, add it.
                            Invariant.Assert( rewrittenProperty.IsAutoPropertyDeclaration() );
                            Invariant.Assert( rewrittenProperty.AccessorList != null );

                            Invariant.Assert(
                                !rewrittenProperty.AccessorList!.Accessors.Any(
                                    a => a.IsKind( SyntaxKind.SetAccessorDeclaration ) || a.IsKind( SyntaxKind.InitAccessorDeclaration ) )
                                || rewrittenProperty.AccessorList!.Accessors.Any( a => a.IsKind( SyntaxKind.InitAccessorDeclaration ) ) );

                            rewritten =
                                rewrittenProperty.WithAccessorList(
                                    rewrittenProperty.AccessorList.WithAccessors(
                                        List(
                                            rewrittenProperty.AccessorList.Accessors
                                                .Where( a => !a.IsKind( SyntaxKind.InitAccessorDeclaration ) )
                                                .Append(
                                                    AccessorDeclaration(
                                                            SyntaxKind.SetAccessorDeclaration,
                                                            List<AttributeListSyntax>(),
                                                            default,
                                                            null,
                                                            null )
                                                        .WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) ) ) );

                            // If the property implicitly implements interface members, we need to emit explicit implementations with init-only setter.
                            if ( this._currentTypeImplicitInterfaceImplementations.AssertNotNull()
                                .TryGetValue( propertySymbol, out var implicitlyImplementedInterfaceMembers ) )
                            {
                                foreach ( var interfaceProperty in implicitlyImplementedInterfaceMembers.OfType<IPropertySymbol>() )
                                {
                                    var interfaceScope = this.SymbolClassifier.GetTemplatingScope( interfaceProperty.ContainingType );

                                    if ( interfaceScope == TemplatingScope.RunTimeOnly )
                                    {
                                        // Do not generate explicit implementation for runtime interfaces.
                                        continue;
                                    }

                                    if ( interfaceProperty.SetMethod is not { IsInitOnly: true } )
                                    {
                                        continue;
                                    }

                                    // If the property implicitly implements any interface property with init accessor, we need to add explicit implementation because
                                    // changing it to ordinary setter would cause an error.
                                    yield return
                                        PropertyDeclaration(
                                            List<AttributeListSyntax>(),
                                            TokenList(),
                                            rewrittenProperty.Type,
                                            ExplicitInterfaceSpecifier(
                                                (NameSyntax) this._syntaxGenerationContext.SyntaxGenerator.TypeSyntax( interfaceProperty.ContainingType ) ),
                                            rewrittenProperty.Identifier,
                                            AccessorList(
                                                List(
                                                    new[]
                                                    {
                                                        interfaceProperty.GetMethod != null
                                                            ? AccessorDeclaration(
                                                                SyntaxKind.GetAccessorDeclaration,
                                                                List<AttributeListSyntax>(),
                                                                TokenList(),
                                                                Token( SyntaxKind.GetKeyword ),
                                                                null,
                                                                ArrowExpressionClause(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        ThisExpression(),
                                                                        IdentifierName( interfaceProperty.Name ) ) ),
                                                                Token( SyntaxKind.SemicolonToken ) )
                                                            : null,
                                                        AccessorDeclaration(
                                                            SyntaxKind.InitAccessorDeclaration,
                                                            List<AttributeListSyntax>(),
                                                            TokenList(),
                                                            Token( SyntaxKind.InitKeyword ),
                                                            null,
                                                            ArrowExpressionClause(
                                                                AssignmentExpression(
                                                                    SyntaxKind.SimpleAssignmentExpression,
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        ThisExpression(),
                                                                        IdentifierName( interfaceProperty.Name ) ),
                                                                    IdentifierName( "value" ) ) ),
                                                            Token( SyntaxKind.SemicolonToken ) )
                                                    }.WhereNotNull() ) ) );
                                }
                            }
                        }

                        yield return rewritten;
                    }
                    else if ( propertySymbol.IsOverride && propertySymbol.OverriddenProperty!.IsAbstract )
                    {
                        yield return this._helper.WithThrowNotSupportedExceptionBody( node, "Template code cannot be directly executed." );
                    }
                    else if ( propertySymbol.IsAbstract )
                    {
                        if ( !this.SymbolClassifier.GetTemplatingScope( propertySymbol.Type ).CanExecuteAtCompileTime()
                             || propertySymbol.Parameters.Any( p => !this.SymbolClassifier.GetTemplatingScope( p.Type ).CanExecuteAtCompileTime() ) )
                        {
                            this._diagnosticAdder.Report(
                                TemplatingDiagnosticDescriptors.AbstractTemplateCannotHaveRunTimeSignature.CreateRoslynDiagnostic(
                                    propertySymbol.GetDiagnosticLocation(),
                                    propertySymbol ) );
                        }
                        else
                        {
                            yield return node;
                        }
                    }
                    else
                    {
                        // The property can be deleted, i.e. it does not need to be inserted back in the member list.
                    }

                    if ( transformedGetDeclaration != null )
                    {
                        yield return (MemberDeclarationSyntax) transformedGetDeclaration;
                    }

                    if ( transformedSetDeclaration != null )
                    {
                        yield return (MemberDeclarationSyntax) transformedSetDeclaration;
                    }
                }
                else
                {
                    this.Success = false;
                }
            }

            private IEnumerable<MemberDeclarationSyntax> TransformFieldDeclaration( FieldDeclarationSyntax node )
            {
                foreach ( var declarator in node.Declaration.Variables )
                {
                    var fieldSymbol = (IFieldSymbol?) this.RunTimeSemanticModelProvider.GetSemanticModel( declarator.SyntaxTree )
                        .GetDeclaredSymbol( declarator );

                    if ( fieldSymbol == null || this.ShouldExcludeMember( fieldSymbol ) )
                    {
                        yield break;
                    }

                    var removeReadOnly = this._serializableFieldsAndProperties.TryGetValue( fieldSymbol, out var serializableType )
                                         && this._serializerGenerator.ShouldSuppressReadOnly( serializableType, fieldSymbol );

                    // This field needs to have their readonly modifier removed, so add it to the list.
                    foreach ( var result in this.TransformFieldOrEventVariable(
                                 TemplateCompilerSemantics.Initializer,
                                 declarator,
                                 v =>
                                 {
                                     var member = node.WithDeclaration(
                                             node.Declaration.WithVariables( SingletonSeparatedList( v ) )
                                                 .WithType( (TypeSyntax) this.Visit( node.Declaration.Type )! ) )
                                         .WithAttributeLists( this.VisitAttributeLists( node.AttributeLists ) );

                                     if ( removeReadOnly )
                                     {
                                         member = member.WithModifiers( TokenList( node.Modifiers.Where( m => !m.IsKind( SyntaxKind.ReadOnlyKeyword ) ) ) );
                                     }

                                     return member;
                                 } ) )
                    {
                        yield return result;
                    }
                }
            }

            private IEnumerable<MemberDeclarationSyntax> TransformEventFieldDeclaration( EventFieldDeclarationSyntax node )
            {
                foreach ( var declarator in node.Declaration.Variables )
                {
                    foreach ( var result in this.TransformFieldOrEventVariable(
                                 TemplateCompilerSemantics.Initializer,
                                 declarator,
                                 v => node.WithDeclaration(
                                         node.Declaration.WithVariables( SingletonSeparatedList( v ) )
                                             .WithType( (TypeSyntax) this.Visit( node.Declaration.Type )! ) )
                                     .WithAttributeLists( this.VisitAttributeLists( node.AttributeLists ) ) ) )
                    {
                        yield return result;
                    }
                }
            }

            private IEnumerable<MemberDeclarationSyntax> TransformFieldOrEventVariable(
                TemplateCompilerSemantics templateSyntaxKind,
                VariableDeclaratorSyntax variable,
                Func<VariableDeclaratorSyntax, MemberDeclarationSyntax> createMember )
            {
                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( variable.SyntaxTree ).GetDeclaredSymbol( variable );

                if ( symbol == null || this.ShouldExcludeMember( symbol ) )
                {
                    yield break;
                }

                var templateInfo = this.SymbolClassifier.GetTemplateInfo( symbol );

                this.AddToManifestIfNecessary( symbol, templateInfo );

                if ( templateInfo.HasNoBody )
                {
                    yield break;
                }

                var isTemplate = !templateInfo.IsNone;

                if ( isTemplate && variable.Initializer != null )
                {
                    var templateName = TemplateNameHelper.GetCompiledTemplateName( symbol );

                    // This is field template with initializer.
                    if ( this._templateCompiler.TryCompile(
                            templateName,
                            this._compileTimeCompilation,
                            variable,
                            templateSyntaxKind,
                            this.RunTimeSemanticModelProvider.GetSemanticModel( variable.SyntaxTree ),
                            this._diagnosticAdder,
                            this._cancellationToken,
                            out _,
                            out var transformedFieldDeclaration,
                            out var usedApiVersion ) )
                    {
                        this.AddToManifest( symbol, usedApiVersion );

                        yield return (MethodDeclarationSyntax) transformedFieldDeclaration;
                    }
                    else
                    {
                        this.Success = false;
                    }
                }
                else
                {
                    var variableType = symbol switch
                    {
                        IEventSymbol @eventSymbol => @eventSymbol.Type,
                        IFieldSymbol fieldSymbol => fieldSymbol.Type,
                        _ => throw new AssertionFailedException( $"Unexpected symbol kind: {symbol.Kind}." )
                    };

                    if ( this.SymbolClassifier.GetTemplatingScope( variableType ).CanExecuteAtCompileTime() )
                    {
                        yield return createMember( (VariableDeclaratorSyntax) this.Visit( variable ).AssertNotNull() );
                    }
                }

                if ( isTemplate && symbol.IsAbstract )
                {
                    yield return createMember( variable );
                }
            }

            private IEnumerable<MemberDeclarationSyntax> TransformEventDeclaration( EventDeclarationSyntax node )
            {
                var eventSymbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node );

                if ( eventSymbol == null || this.ShouldExcludeMember( eventSymbol ) )
                {
                    yield break;
                }

                var templateInfo = this.SymbolClassifier.GetTemplateInfo( eventSymbol );
                this.AddToManifestIfNecessary( eventSymbol, templateInfo, null, eventSymbol.AddMethod, eventSymbol.RemoveMethod );

                if ( templateInfo.IsNone )
                {
                    yield return (BasePropertyDeclarationSyntax) this.Visit( node ).AssertNotNull();

                    yield break;
                }

                if ( !this.CheckTemplateName( eventSymbol ) )
                {
                    yield break;
                }

                var success = true;
                SyntaxNode? transformedAddDeclaration = null;
                SyntaxNode? transformedRemoveDeclaration = null;

                // Compile accessors into templates.
                if ( !eventSymbol.IsAbstract )
                {
                    if ( node.AccessorList != null )
                    {
                        var addAccessor = node.AccessorList.Accessors.Single( a => a.Kind() == SyntaxKind.AddAccessorDeclaration );
                        var removeAccessor = node.AccessorList.Accessors.Single( a => a.Kind() == SyntaxKind.RemoveAccessorDeclaration );

                        RoslynApiVersion? maxApiVersion = null;

                        if ( success )
                        {
                            success =
                                this._templateCompiler.TryCompile(
                                    TemplateNameHelper.GetCompiledTemplateName( eventSymbol.AddMethod.AssertNotNull() ),
                                    this._compileTimeCompilation,
                                    addAccessor,
                                    TemplateCompilerSemantics.Default,
                                    this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                    this._diagnosticAdder,
                                    this._cancellationToken,
                                    out _,
                                    out transformedAddDeclaration,
                                    out var addApiVersion );

                            maxApiVersion ??= addApiVersion;
                        }

                        if ( success )
                        {
                            success =
                                this._templateCompiler.TryCompile(
                                    TemplateNameHelper.GetCompiledTemplateName( eventSymbol.RemoveMethod.AssertNotNull() ),
                                    this._compileTimeCompilation,
                                    removeAccessor,
                                    TemplateCompilerSemantics.Default,
                                    this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ),
                                    this._diagnosticAdder,
                                    this._cancellationToken,
                                    out _,
                                    out transformedRemoveDeclaration,
                                    out var removeApiVersion );

                            if ( maxApiVersion == null || removeApiVersion > maxApiVersion )
                            {
                                maxApiVersion = removeApiVersion;
                            }
                        }

                        this.AddToManifest( eventSymbol, maxApiVersion );
                    }
                }

                if ( success )
                {
                    if ( eventSymbol.IsOverride && eventSymbol.OverriddenEvent!.IsAbstract )
                    {
                        yield return this._helper.WithThrowNotSupportedExceptionBody( node, "Template code cannot be directly executed." );
                    }

                    // Note: EventDeclarationSyntax can't be abstract, only EventFieldDeclarationSyntax can.

                    if ( transformedAddDeclaration != null )
                    {
                        yield return (MemberDeclarationSyntax) transformedAddDeclaration;
                    }

                    if ( transformedRemoveDeclaration != null )
                    {
                        yield return (MemberDeclarationSyntax) transformedRemoveDeclaration;
                    }
                }
                else
                {
                    this.Success = false;
                }
            }

            private SyntaxList<MemberDeclarationSyntax> VisitTypeOrNamespaceMembers( IReadOnlyList<MemberDeclarationSyntax> members )
            {
                var resultingMembers = new List<MemberDeclarationSyntax>( members.Count );

                foreach ( var member in members )
                {
                    switch ( member )
                    {
                        case TypeDeclarationSyntax type:
                            resultingMembers.AddRange( this.VisitTypeDeclaration( type ) );

                            break;

                        default:
                            var transformedMember = (MemberDeclarationSyntax?) this.Visit( member );

                            if ( transformedMember != null )
                            {
                                resultingMembers.Add( transformedMember );
                            }

                            break;
                    }
                }

                return List( resultingMembers );
            }

            public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            {
                var unnestedType = this._currentContext.NestedType;

                var visitedConstructor = (ConstructorDeclarationSyntax) base.VisitConstructorDeclaration( node )!;

                if ( unnestedType != null && node.Identifier.Text == unnestedType.Name )
                {
                    return visitedConstructor.WithIdentifier( Identifier( this._currentContext.NestedTypeNewName! ) );
                }
                else
                {
                    return visitedConstructor;
                }
            }

            public override SyntaxNode? VisitNamespaceDeclaration( NamespaceDeclarationSyntax node )
            {
                var transformedMembers = this.VisitTypeOrNamespaceMembers( node.Members );

                if ( transformedMembers.Any( m => m.HasAnnotation( _hasCompileTimeCodeAnnotation ) ) )
                {
                    return node.WithMembers( transformedMembers )
                        .WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation );
                }
                else
                {
                    return null;
                }
            }

            public override SyntaxNode? VisitFileScopedNamespaceDeclaration( FileScopedNamespaceDeclarationSyntax node )
            {
                var transformedMembers = this.VisitTypeOrNamespaceMembers( node.Members );

                if ( transformedMembers.Any( m => m.HasAnnotation( _hasCompileTimeCodeAnnotation ) ) )
                {
                    return node.WithMembers( transformedMembers )
                        .WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation );
                }
                else
                {
                    return null;
                }
            }

            public override SyntaxNode VisitCompilationUnit( CompilationUnitSyntax node )
            {
                // Get the list of members that are not statements, local variables, local functions,...
                var nonTopLevelMembers = node.Members.Where(
                        m => m is BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax or DelegateDeclarationSyntax or FileScopedNamespaceDeclarationSyntax )
                    .ToReadOnlyList();

                var transformedMembers = this.VisitTypeOrNamespaceMembers( nonTopLevelMembers );

                if ( transformedMembers.Any( m => m.HasAnnotation( _hasCompileTimeCodeAnnotation ) ) )
                {
                    // Filter usings. It is important to visit all nodes so we also process preprocessor directives.
                    var currentUsings = node.Usings.SelectAsReadOnlyList( n => n.ToString() ).ToHashSet();

                    var usings = this._globalUsings.Where( u => !currentUsings.Contains( u.ToString() ) )
                        .Select( u => u.WithGlobalKeyword( default ) )
                        .Concat( node.Usings.SelectAsReadOnlyList( x => this.Visit( x ).AssertCast<UsingDirectiveSyntax>() ).WhereNotNull() );

                    // Filter attributes. It is important to visit all nodes so we also process preprocessor directives.
                    var attributes = this.VisitAttributeLists( node.AttributeLists );

                    return node.WithMembers( transformedMembers )
                        .WithAdditionalAnnotations( _hasCompileTimeCodeAnnotation )
                        .WithUsings( List( usings ) )
                        .WithAttributeLists( attributes );
                }
                else
                {
                    // The rewriter should not have been invoked in a compilation unit that does not
                    // contain any build-time code. However, the compilation unit can contain only illegitimate compile-time
                    // code which has been stripped. In this case, we return an empty compilation unit.

                    return CompilationUnit( default, default, default, default );
                }
            }

            public override SyntaxNode? VisitUsingDirective( UsingDirectiveSyntax node )
            {
#if ROSLYN_4_8_0_OR_GREATER
                if ( !node.UnsafeKeyword.IsKind( SyntaxKind.None ) )
                {
                    return null;
                }
#endif

                return base.VisitUsingDirective( node );
            }

            public override SyntaxNode? VisitInvocationExpression( InvocationExpressionSyntax node )
            {
                if ( this._currentContext.Scope != TemplatingScope.RunTimeOnly && node.IsNameOf() )
                {
                    var symbolInfo = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree )
                        .GetSymbolInfo( node.ArgumentList.Arguments[0].Expression );

                    var typeSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                    if ( typeSymbol != null )
                    {
                        return SyntaxFactoryEx.LiteralExpression( typeSymbol.Name );
                    }
                }

                return base.VisitInvocationExpression( node );
            }

            public override SyntaxNode? VisitTypeOfExpression( TypeOfExpressionSyntax node )
            {
                if ( this._currentContext.Scope != TemplatingScope.RunTimeOnly )
                {
                    var typeSymbol = (ITypeSymbol?) this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetSymbolInfo( node.Type ).Symbol;

                    if ( typeSymbol != null )
                    {
                        if ( this.SymbolClassifier.GetTemplatingScope( typeSymbol ) == TemplatingScope.RunTimeOnly )
                        {
                            return this._typeOfRewriter.RewriteTypeOf( typeSymbol );
                        }
                    }
                }

                return base.VisitTypeOfExpression( node );
            }

            private T? AddLocationAnnotation<T>( T? originalNode, T? transformedNode )
                where T : SyntaxNode
                => originalNode == null || transformedNode == null
                    ? null
                    : (T?) this._templateCompiler.LocationAnnotationMap.AddLocationAnnotation( originalNode, transformedNode );

            // The default implementation of Visit(SyntaxNode) and Visit(SyntaxToken) adds the location annotations.

            protected override SyntaxNode? VisitCore( SyntaxNode? node ) => this.AddLocationAnnotation( node, base.VisitCore( node ) );

            public override SyntaxToken VisitToken( SyntaxToken token )
            {
                var tokenWithoutPreprocessorDirectives = base.VisitToken( token );

                return this._templateCompiler.LocationAnnotationMap.AddLocationAnnotation( tokenWithoutPreprocessorDirectives );
            }

            public override SyntaxNode VisitInterpolation( InterpolationSyntax node )
                => InterpolationSyntaxHelper.Fix( (InterpolationSyntax) base.VisitInterpolation( node ).AssertNotNull() );

            private static SyntaxToken GetConstructorNameToken( NameSyntax typeName )
                => typeName switch
                {
                    AliasQualifiedNameSyntax aliasQualifiedNameSyntax => aliasQualifiedNameSyntax.Name.Identifier,
                    QualifiedNameSyntax qualifiedNameSyntax => qualifiedNameSyntax.Right.Identifier,
                    SimpleNameSyntax simpleNameSyntax => simpleNameSyntax.Identifier,
                    _ => throw new AssertionFailedException( $"Unexpected syntax kind {typeName.Kind()} at '{typeName.GetLocation()}'." )
                };

            private TypeSyntax CreateTypeSyntax( INamespaceOrTypeSymbol symbol )
            {
                var unnestedType = this._currentContext.NestedType;
                var type = this._syntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( symbol );

                static NameSyntax RenameType( NameSyntax syntax, string newIdentifier, int nestingLevel )
                    => syntax switch
                    {
                        AliasQualifiedNameSyntax aliasQualifiedNameSyntax => aliasQualifiedNameSyntax.WithName( IdentifierName( newIdentifier ) ),
                        QualifiedNameSyntax qualifiedNameSyntax when nestingLevel > 0 => RenameType(
                            qualifiedNameSyntax.Left,
                            newIdentifier,
                            nestingLevel - 1 ),
                        QualifiedNameSyntax qualifiedNameSyntax when nestingLevel == 0 => qualifiedNameSyntax.WithRight( IdentifierName( newIdentifier ) ),
                        SimpleNameSyntax => IdentifierName( newIdentifier ),
                        _ => throw new AssertionFailedException( $"Unexpected syntax kind {syntax.Kind()} at '{syntax.GetDiagnosticLocation()}'." )
                    };

                if ( symbol.Equals( unnestedType ) )
                {
                    if ( type is not NameSyntax typeName )
                    {
                        throw new AssertionFailedException( $"Attempting to rename type '{type}' that doesn't have a name." );
                    }

                    return RenameType( typeName, this._currentContext.NestedTypeNewName!, this._currentContext.NestingLevel );
                }

                return type;
            }

            public override SyntaxNode? VisitQualifiedName( QualifiedNameSyntax node )
            {
                // Fully qualify type names and namespaces.

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetSymbolInfo( node ).Symbol;

                if ( symbol is INamespaceOrTypeSymbol namespaceOrType )
                {
                    var nodeWithoutPreprocessorDirectives = base.VisitQualifiedName( node ).AssertNotNull();

                    return this.CreateTypeSyntax( namespaceOrType ).WithTriviaFrom( nodeWithoutPreprocessorDirectives );
                }

                return base.VisitQualifiedName( node );
            }

            public override SyntaxNode? VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
            {
                // Fully qualify type names and namespaces.

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetSymbolInfo( node ).Symbol;

                if ( symbol is INamespaceOrTypeSymbol namespaceOrType )
                {
                    var nodeWithoutPreprocessorDirectives = base.VisitMemberAccessExpression( node ).AssertNotNull();

                    return this.CreateTypeSyntax( namespaceOrType ).WithTriviaFrom( nodeWithoutPreprocessorDirectives );
                }

                return base.VisitMemberAccessExpression( node );
            }

            public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
            {
                var nodeWithoutPreprocessorDirectives = base.VisitIdentifierName( node ).AssertNotNull();

                if ( node.Identifier.Text == "dynamic" )
                {
                    return PredefinedType( Token( SyntaxKind.ObjectKeyword ) ).WithTriviaFrom( nodeWithoutPreprocessorDirectives );
                }

                var symbol = this.RunTimeSemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetSymbolInfo( node ).Symbol;

                // Detect references to Metalama.Framework.Sdk.
                if ( !this.ReferencesMetalamaSdk
                     && symbol?.ContainingAssembly?.Name.Equals( "metalama.framework.sdk", StringComparison.OrdinalIgnoreCase ) == true )
                {
                    this.ReferencesMetalamaSdk = true;
                }

                // Fully qualifies simple identifiers.
                if ( node.Identifier.IsKind( SyntaxKind.IdentifierToken )
                     && node is { IsVar: false, Parent: not (QualifiedNameSyntax or AliasQualifiedNameSyntax) } &&
                     !(node.Parent is MemberAccessExpressionSyntax memberAccessExpressionSyntax
                       && node == memberAccessExpressionSyntax.Name) )
                {
                    switch ( symbol )
                    {
                        case INamespaceOrTypeSymbol namespaceOrType:
                            return this.CreateTypeSyntax( namespaceOrType ).WithTriviaFrom( nodeWithoutPreprocessorDirectives );

                        case { IsStatic: true }
                            when node.Parent is not MemberAccessExpressionSyntax
                                 && node.Parent is not AliasQualifiedNameSyntax
                                 && symbol is not IMethodSymbol { MethodKind: MethodKind.LocalFunction }:
                            switch ( symbol.Kind )
                            {
                                case SymbolKind.Field:
                                case SymbolKind.Property:
                                case SymbolKind.Event:
                                case SymbolKind.Method:
                                    // We have an access to a field or method with a "using static", or a non-qualified static member access.
                                    return MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            this.CreateTypeSyntax( symbol.ContainingType ),
                                            IdentifierName( node.Identifier.Text ) )
                                        .WithTriviaFrom( nodeWithoutPreprocessorDirectives );
                            }

                            break;
                    }
                }

                return base.VisitIdentifierName( node );
            }

            private Context WithScope( TemplatingScope scope )
            {
                this._currentContext = new Context(
                    scope,
                    this._currentContext.NestedType,
                    this._currentContext.NestedTypeNewName,
                    this._currentContext.NestingLevel,
                    this );

                return this._currentContext;
            }

            public override SyntaxTrivia VisitTrivia( SyntaxTrivia trivia )
                => trivia.Kind() switch
                {
                    SyntaxKind.MultiLineCommentTrivia => default,
                    SyntaxKind.SingleLineCommentTrivia => default,
                    SyntaxKind.MultiLineDocumentationCommentTrivia => default,
                    SyntaxKind.SingleLineDocumentationCommentTrivia => default,
                    _ => trivia
                };

            private Context WithUnnestedType( INamedTypeSymbol unnestedType, string newName, int nestingLevel )
            {
                this._currentContext = new Context( this._currentContext.Scope, unnestedType, newName, nestingLevel, this );

                return this._currentContext;
            }

            public TemplateProjectManifest GetManifest() => this._compileTimeManifestBuilder.Build();
        }
    }
}