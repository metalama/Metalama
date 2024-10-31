// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Templating
{
    public partial class TemplatingCodeValidator
    {
        /// <summary>
        /// Performs the analysis that are not performed by the pipeline: essentially validates that run-time code does not
        /// reference compile-time-only code, and run the template compiler.
        /// </summary>
        private sealed class Visitor : SafeSyntaxWalker, IDiagnosticAdder
        {
            private readonly ISymbolClassifier _classifier;
            private readonly HashSet<ISymbol> _alreadyReportedDiagnostics = new( SymbolEqualityComparer.Default );
            private readonly bool _reportCompileTimeTreeOutdatedError;
            private readonly bool _isDesignTime;
            private readonly ProjectServiceProvider _serviceProvider;
            private readonly SemanticModel _semanticModel;
            private readonly ClassifyingCompilationContext _compilationContext;
            private readonly Action<Diagnostic> _reportDiagnostic;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _hasCompileTimeCodeFast;
            private readonly ITypeSymbol _typeFabricType;
            private readonly ITypeSymbol _iAdviceAttributeType;
            private readonly ITypeSymbol _iCompileTimeSerializableType;
            private TemplateCompiler? _templateCompiler;

            private ISymbol? _currentDeclaration;
            private TemplatingScope? _currentScope;
            private TemplatingScope? _currentTypeScope;
            private TemplateInfo? _currentTemplateInfo;

            public bool HasError { get; private set; }

            public Visitor(
                ProjectServiceProvider serviceProvider,
                SemanticModel semanticModel,
                ClassifyingCompilationContext compilationContext,
                Action<Diagnostic> reportDiagnostic,
                bool reportCompileTimeTreeOutdatedError,
                bool isDesignTime,
                CancellationToken cancellationToken )
            {
                this._serviceProvider = serviceProvider;
                this._semanticModel = semanticModel;
                this._compilationContext = compilationContext;
                this._reportDiagnostic = reportDiagnostic;
                this._classifier = compilationContext.SymbolClassifier;
                this._reportCompileTimeTreeOutdatedError = reportCompileTimeTreeOutdatedError;
                this._isDesignTime = isDesignTime;
                this._cancellationToken = cancellationToken;
                this._hasCompileTimeCodeFast = CompileTimeCodeFastDetector.HasCompileTimeCode( semanticModel.SyntaxTree.GetRoot() );
                this._typeFabricType = compilationContext.ReflectionMapper.GetTypeSymbol( typeof(TypeFabric) );
                this._iAdviceAttributeType = compilationContext.ReflectionMapper.GetTypeSymbol( typeof(IAdviceAttribute) );
                this._iCompileTimeSerializableType = compilationContext.ReflectionMapper.GetTypeSymbol( typeof(ICompileTimeSerializable) );
            }

            private bool IsInTemplate => this._currentTemplateInfo is { AttributeType: not TemplateAttributeType.None };

            protected override void VisitCore( SyntaxNode? node )
            {
                bool IsTypeOfOrNameOf()
                {
                    return node
                        .AncestorsAndSelf()
                        .Any( n => n is TypeOfExpressionSyntax || (n is InvocationExpressionSyntax invocation && invocation.IsNameOf()) );
                }

                bool AvoidDuplicates( ISymbol symbol )
                {
                    return this._alreadyReportedDiagnostics.Add( symbol ) &&
                           !(symbol.ContainingSymbol != null
                             && this._alreadyReportedDiagnostics.Contains( symbol.ContainingSymbol ));
                }

                if ( node is null or IdentifierNameSyntax { IsVar: true } )
                {
                    // We skip 'var' because the semantic model sometimes resolve it to dynamic for no reason,
                    // and there is little value in spending more effort coping with this case.
                    return;
                }

                this._cancellationToken.ThrowIfCancellationRequested();

                // We want children to be processed before parents, so that errors are reported on parent (declaring) symbols.
                // This allows to reduce redundant messages.
                base.VisitCore( node );

                // If the scope is null (e.g. in a using directive), we should not analyze.
                if ( !this._currentScope.HasValue )
                {
                    return;
                }

                // Otherwise, we have to check references.

                var referencedSymbol = this._semanticModel.GetSymbolInfo( node ).Symbol;

                if ( referencedSymbol is not null and not ITypeParameterSymbol )
                {
                    var referencedScope = this._classifier.GetTemplatingScope( referencedSymbol );

                    if ( referencedScope.GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly )
                    {
                        if ( !this.IsInTemplate && !IsTypeOfOrNameOf() )
                        {
                            if ( this._classifier.IsTemplateOnly( referencedSymbol ) )
                            {
                                // Cannot reference template-only symbol outside of a template, except in a nameof().
                                if ( AvoidDuplicates( referencedSymbol ) )
                                {
                                    string? explanation = null;

                                    switch (referencedSymbol.ContainingType.GetReflectionFullName(), referencedSymbol.Name)
                                    {
                                        case ("Metalama.Framework.Aspects.meta", "This"):
                                            explanation = " Use ExpressionFactory.This() instead of meta.This.";

                                            break;

                                        case ("Metalama.Framework.Code.IExpression", "Value"):
                                            var expression = node.Parent is MemberAccessExpressionSyntax memberAccess
                                                ? memberAccess.Expression.ToString()
                                                : "expression";

                                            explanation = $" Use '{expression}' directly instead of accessing '{expression}.Value'.";

                                            break;

                                        case ("Metalama.Framework.Code.SyntaxBuilders.SyntaxBuilder", "AppendExpression"):
                                            if ( node.Parent?.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } )
                                            {
                                                var argumentType = this._semanticModel.GetTypeInfo( argument.Expression ).Type;

                                                if ( IsLiteralType( argumentType ) )
                                                {
                                                    explanation =
                                                        $" Use 'AppendLiteral({argument.Expression})' instead of 'AppendExpression({argument.Expression})'.";
                                                }
                                            }

                                            break;
                                    }

                                    this.Report(
                                        TemplatingDiagnosticDescriptors.CannotUseTemplateOnlyOutOfTemplate.CreateRoslynDiagnostic(
                                            node.GetLocation(),
                                            (this._currentDeclaration!, referencedSymbol, explanation) ) );

                                    static bool IsLiteralType( ITypeSymbol? type )
                                        => type?.GetReflectionFullName() is "System.Int32" or "System.UInt32"
                                            or "System.Int16" or "System.UInt16" or "System.Int64" or "System.UInt64" or "System.Byte" or "System.SByte"
                                            or "System.Double" or "System.Single" or "System.Decimal" or "System.String";
                                }
                            }
                            else if ( !this._currentScope.Value.MustExecuteAtCompileTime() )
                            {
                                // We cannot reference a compile-time-only declaration, except in a typeof() or nameof() expression
                                // because these are transformed by the CompileTimeCompilationBuilder.

                                if ( AvoidDuplicates( referencedSymbol ) )
                                {
                                    this.Report(
                                        TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.CreateRoslynDiagnostic(
                                            node.GetLocation(),
                                            (this._currentDeclaration!, referencedSymbol, this._currentScope.Value) ) );
                                }
                            }
                        }
                    }
                    else if ( referencedScope.GetExpressionExecutionScope() == TemplatingScope.RunTimeOnly )
                    {
                        if ( this._currentScope.Value.GetExpressionExecutionScope() != TemplatingScope.RunTimeOnly && !this.IsInTemplate
                            && !IsTypeOfOrNameOf() )
                        {
                            if ( AvoidDuplicates( referencedSymbol ) )
                            {
                                this.Report(
                                    TemplatingDiagnosticDescriptors.CannotReferenceRunTimeOnly.CreateRoslynDiagnostic(
                                        node.GetLocation(),
                                        (this._currentDeclaration!, referencedSymbol, this._currentScope.Value) ) );
                            }
                        }
                    }
                }
            }

            private void VerifyAttribute( AttributeSyntax node )
            {
                // Currently we're only checking attributes that set members, so do a quick syntactic check first.
                if ( node.ArgumentList?.Arguments.Any( a => a.NameEquals != null ) != true )
                {
                    return;
                }

                var attributeSymbol = (this._semanticModel.GetSymbolInfo( node ).Symbol as IMethodSymbol)?.ContainingType;
                var iAspectSymbol = this._compilationContext.ReflectionMapper.GetTypeSymbol( typeof(IAspect) );

                var compilation = this._compilationContext.SourceCompilation;

                if ( compilation.HasImplicitConversion( attributeSymbol, iAspectSymbol ) )
                {
                    foreach ( var argument in node.ArgumentList.Arguments )
                    {
                        if ( argument.NameEquals != null )
                        {
                            // Check that we are not setting a template property or introduced field.
                            var memberSymbol = this._semanticModel.GetSymbolInfo( argument.NameEquals.Name ).Symbol;
                            var templateAttribute = this._compilationContext.ReflectionMapper.GetTypeSymbol( typeof(ITemplateAttribute) );

                            if ( memberSymbol?.GetAttributes().Any( a => compilation.HasImplicitConversion( a.AttributeClass, templateAttribute ) ) == true )
                            {
                                this.Report(
                                    TemplatingDiagnosticDescriptors.CannotSetTemplateMemberFromAttribute.CreateRoslynDiagnostic(
                                        argument.NameEquals.GetDiagnosticLocation(),
                                        memberSymbol.Name ) );
                            }
                        }
                    }
                }
            }

            public override void VisitAttributeList( AttributeListSyntax node )
            {
                // Do not perform regular validation on attributes, except for checks that are specifically for attributes.

                foreach ( var attribute in node.Attributes )
                {
                    this.VerifyAttribute( attribute );
                }
            }

            public override void VisitBaseList( BaseListSyntax node )
            {
                // Do not validate the base list.
            }

            public override void VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                using var context = this.WithDeclaration( node );

                this.VerifyTypeDeclaration( node, context );
                base.VisitClassDeclaration( node );
            }

            public override void VisitStructDeclaration( StructDeclarationSyntax node )
            {
                using var context = this.WithDeclaration( node );

                this.VerifyTypeDeclaration( node, context );
                base.VisitStructDeclaration( node );
            }

            public override void VisitRecordDeclaration( RecordDeclarationSyntax node )
            {
                using var context = this.WithDeclaration( node );

                this.VerifyTypeDeclaration( node, context );
                base.VisitRecordDeclaration( node );
            }

            public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
            {
                using var context = this.WithDeclaration( node );

                this.VerifyTypeDeclaration( node, context );

                base.VisitInterfaceDeclaration( node );
            }

            private void VerifyTypeDeclaration( BaseTypeDeclarationSyntax node, in Context context )
            {
                // Report an error on aspect classes when the pipeline is paused.
                if ( this._currentScope != TemplatingScope.RunTimeOnly && this._reportCompileTimeTreeOutdatedError )
                {
                    this.Report(
                        TemplatingDiagnosticDescriptors.CompileTimeTypeNeedsRebuild.CreateRoslynDiagnostic(
                            node.Identifier.GetLocation(),
                            context.DeclaredSymbol! ) );
                }

                this.VerifyModifiers( node.Modifiers );

                // Verify that the base class and implemented interfaces are scope-compatible.
                // If the scope is conflict, an error message is written elsewhere.

                if ( node.BaseList != null && this._currentScope != TemplatingScope.Conflict )
                {
                    foreach ( var baseTypeNode in node.BaseList.Types )
                    {
                        if ( ModelExtensions.GetSymbolInfo( this._semanticModel, baseTypeNode.Type ).Symbol is not INamedTypeSymbol baseType )
                        {
                            continue;
                        }

                        var baseTypeScope = this._classifier.GetTemplatingScope( baseType );

                        if ( baseTypeScope is TemplatingScope.Conflict )
                        {
                            this._classifier.ReportScopeError( baseTypeNode, baseType, this );
                        }
                        else
                        {
                            var isAcceptableScope = (this._currentScope, scope: baseTypeScope) switch
                            {
                                (TemplatingScope.CompileTimeOnly, TemplatingScope.CompileTimeOnly) => true,
                                (TemplatingScope.CompileTimeOnly, TemplatingScope.RunTimeOrCompileTime) => true,
                                (TemplatingScope.RunTimeOnly, TemplatingScope.RunTimeOnly) => true,
                                (TemplatingScope.RunTimeOnly, TemplatingScope.DynamicTypeConstruction) => true,
                                (TemplatingScope.RunTimeOnly, TemplatingScope.RunTimeOrCompileTime) => true,
                                (TemplatingScope.RunTimeOrCompileTime, _) => true,
                                _ => false
                            };

                            if ( !isAcceptableScope )
                            {
                                this.Report(
                                    TemplatingDiagnosticDescriptors.BaseTypeScopeConflict.CreateRoslynDiagnostic(
                                        baseTypeNode.Type.GetLocation(),
                                        ((INamedTypeSymbol) context.DeclaredSymbol!, this._currentScope!.Value.ToDisplayString(), baseType,
                                         baseTypeScope.ToDisplayString()) ) );
                            }
                        }
                    }
                }

                var symbol = this._semanticModel.GetDeclaredSymbol( node );

#if ROSLYN_4_8_0_OR_GREATER
                if ( symbol is not null
                     && this._currentScope is TemplatingScope.RunTimeOrCompileTime or TemplatingScope.CompileTimeOnly
                     && node is TypeDeclarationSyntax { ParameterList: not null } and (ClassDeclarationSyntax or StructDeclarationSyntax) )
                {
                    // C#12 primary constructors (non-record types) are not supported.
                    this.Report(
                        TemplatingDiagnosticDescriptors.NonRecordPrimaryConstructorsNotSupported.CreateRoslynDiagnostic(
                            node.Identifier.GetLocation(),
                            symbol ) );
                }
#endif

                // Verify serialization conditions.
                if ( symbol is not null
                     && this._compilationContext.SourceCompilation.HasImplicitConversion( symbol, this._iCompileTimeSerializableType ) )
                {
                    SerializerGeneratorHelper.TryGetSerializer(
                        this._compilationContext.CompilationContext,
                        symbol,
                        out var serializerType,
                        out var ambiguous );

                    if ( ambiguous )
                    {
                        // Ambiguous manual serializer.
                        this.Report(
                            SerializationDiagnosticDescriptors.AmbiguousManualSerializer.CreateRoslynDiagnostic(
                                symbol.GetDiagnosticLocation(),
                                symbol ) );
                    }
                    else if ( serializerType == null && node is RecordDeclarationSyntax { ParameterList.Parameters.Count: > 0 } )
                    {
                        // Generated serializers for positional records are not supported.
                        this.Report(
                            SerializationDiagnosticDescriptors.RecordSerializersNotSupported.CreateRoslynDiagnostic(
                                node.Identifier.GetLocation(),
                                symbol ) );
                    }
                }
            }

            private void VerifyModifiers( SyntaxTokenList modifiers )
            {
                // Forbid unsafe compile-time code.
                var unsafeKeyword = modifiers.FirstOrDefault( m => m.IsKind( SyntaxKind.UnsafeKeyword ) );

                if ( unsafeKeyword.IsKind( SyntaxKind.UnsafeKeyword ) )
                {
                    if ( this._currentTemplateInfo is { IsNone: false } )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.UnsafeCodeForbiddenInTemplate.CreateRoslynDiagnostic(
                                unsafeKeyword.GetLocation(),
                                this._currentDeclaration! ) );
                    }
                    else if ( this._currentScope != TemplatingScope.RunTimeOnly )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.UnsafeCodeForbiddenInCompileTimeCode.CreateRoslynDiagnostic(
                                unsafeKeyword.GetLocation(),
                                (this._currentDeclaration!, this._currentScope!.Value.ToDisplayString()) ) );
                    }
                }

                // Forbid partial templates.
                var partialKeyword = modifiers.FirstOrDefault( m => m.IsKind( SyntaxKind.PartialKeyword ) );

                if ( partialKeyword.IsKind( SyntaxKind.PartialKeyword ) )
                {
                    if ( this._currentTemplateInfo is { IsNone: false } )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.PartialTemplatesForbidden.CreateRoslynDiagnostic(
                                partialKeyword.GetLocation(),
                                this._currentDeclaration! ) );
                    }
                }
            }

            public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
                => this.VisitBaseMethodOrAccessor(
                    node,
                    node.Modifiers,
                    base.VisitMethodDeclaration );

            public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node )
                => this.VisitBaseMethodOrAccessor(
                    node,
                    node.Modifiers,
                    base.VisitAccessorDeclaration );

            private void VisitBaseMethodOrAccessor<T>( T node, SyntaxTokenList modifiers, Action<T> visitBase, ISymbol? declaredSymbol = null )
                where T : SyntaxNode
            {
                using ( this.WithDeclaration( node, declaredSymbol ) )
                {
                    this.VerifyModifiers( modifiers );

                    if ( this.IsInTemplate )
                    {
                        if ( this._isDesignTime && !node.IsKind( SyntaxKind.UnknownAccessorDeclaration ) )
                        {
                            this._templateCompiler ??= new TemplateCompiler( this._serviceProvider, this._compilationContext );
                            _ = this._templateCompiler.TryAnnotate( node, this._semanticModel, this, this._cancellationToken, out _, out _ );
                        }
                        else
                        {
                            // The template compiler will be called by the main pipeline.
                        }
                    }
                    else
                    {
                        visitBase( node );
                    }
                }
            }

            public override void VisitPropertyDeclaration( PropertyDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitPropertyDeclaration( node );
                }
            }

            public override void VisitIndexerDeclaration( IndexerDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitIndexerDeclaration( node );
                }
            }

            public override void VisitArrowExpressionClause( ArrowExpressionClauseSyntax node )
            {
                // For e.g. int P => 42;, there is no node that declares the getter,
                // so we have to handle it manually.
                if ( node.Parent is PropertyDeclarationSyntax propertyDeclaration )
                {
                    var getMethod = this._semanticModel.GetDeclaredSymbol( propertyDeclaration ).AssertSymbolNotNull().GetMethod;
                    this.VisitBaseMethodOrAccessor( node, default, base.VisitArrowExpressionClause, getMethod );
                }
                else
                {
                    base.VisitArrowExpressionClause( node );
                }
            }

            public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitConstructorDeclaration( node );
                }
            }

            public override void VisitDestructorDeclaration( DestructorDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitDestructorDeclaration( node );
                }
            }

            public override void VisitOperatorDeclaration( OperatorDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitOperatorDeclaration( node );
                }
            }

            public override void VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitConversionOperatorDeclaration( node );
                }
            }

            public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    base.VisitLocalFunctionStatement( node );
                }
            }

            public override void VisitEventDeclaration( EventDeclarationSyntax node )
            {
                using ( this.WithDeclaration( node ) )
                {
                    this.VerifyModifiers( node.Modifiers );
                    base.VisitEventDeclaration( node );
                }
            }

            public override void VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
            {
                foreach ( var f in node.Declaration.Variables )
                {
                    using ( this.WithDeclaration( f ) )
                    {
                        this.VerifyModifiers( node.Modifiers );
                        this.Visit( node.Declaration.Type );
                        this.VisitVariableDeclarator( f );
                    }
                }
            }

            public override void VisitFieldDeclaration( FieldDeclarationSyntax node )
            {
                foreach ( var f in node.Declaration.Variables )
                {
                    using ( this.WithDeclaration( f ) )
                    {
                        this.VerifyModifiers( node.Modifiers );
                        this.Visit( node.Declaration.Type );
                        this.VisitVariableDeclarator( f );
                    }
                }
            }

            public override void VisitIncompleteMember( IncompleteMemberSyntax node )
            {
                // Skip
            }

            public void Report( Diagnostic diagnostic )
            {
                if ( diagnostic.Severity == DiagnosticSeverity.Error )
                {
                    this.HasError = true;
                }

                this._reportDiagnostic( diagnostic );
            }

            private Context WithDeclaration( SyntaxNode node, ISymbol? declaredSymbol = null )
            {
                // Reset deduplication.
                this._alreadyReportedDiagnostics.Clear();

                // Get the scope.
                declaredSymbol ??= this._semanticModel.GetDeclaredSymbol( node );

                if ( declaredSymbol == null )
                {
                    return default;
                }

                var scope = this._classifier.GetTemplatingScope( declaredSymbol );

                // Report an error for TypeFabric nested in a compile-time type.
                if ( scope == TemplatingScope.CompileTimeOnly
                     && this._currentTypeScope is TemplatingScope.CompileTimeOnly or TemplatingScope.RunTimeOrCompileTime
                     && this._compilationContext.SourceCompilation.HasImplicitConversion( declaredSymbol as ITypeSymbol, this._typeFabricType ) )
                {
                    this.Report(
                        TemplatingDiagnosticDescriptors.CompileTimeTypesCannotHaveTypeFabrics.CreateRoslynDiagnostic(
                            declaredSymbol.GetDiagnosticLocation(),
                            declaredSymbol ) );
                }

                // Report an error for advice attribute on an accessor.
                if ( declaredSymbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } )
                {
                    var adviceAttribute = declaredSymbol.GetAttributes()
                        .FirstOrDefault(
                            a => this._compilationContext.SourceCompilation.HasImplicitConversion( a.AttributeClass, this._iAdviceAttributeType ) );

                    if ( adviceAttribute != null )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.AdviceAttributeOnAccessor.CreateRoslynDiagnostic(
                                declaredSymbol.GetDiagnosticLocation(),
                                (declaredSymbol, adviceAttribute.AttributeClass.AssertSymbolNotNull(), associatedSymbol.Kind.ToDisplayName()) ) );
                    }
                }

                // Report an error for multiple advice attributes.
                IEnumerable<(ISymbol Member, INamedTypeSymbol AttributeClass)> GetAdviceAttributes( ISymbol? member )
                {
                    if ( member is null or ITypeSymbol )
                    {
                        return [];
                    }

                    var selfAttributes = member.GetAttributes()
                        .Where( a => this._compilationContext.SourceCompilation.HasImplicitConversion( a.AttributeClass, this._iAdviceAttributeType ) )
                        .Select( a => (member, a.AttributeClass!) );

                    var baseAttributesSource = member is IMethodSymbol { AssociatedSymbol: { } memberAssociatedSymbol }
                        ? memberAssociatedSymbol
                        : member.GetOverriddenMember();

                    return selfAttributes.Concat( GetAdviceAttributes( baseAttributesSource ) );
                }

                var adviceAttributes = GetAdviceAttributes( declaredSymbol ).Distinct().Take( 2 ).ToReadOnlyList();

                if ( adviceAttributes.Count > 1 )
                {
                    this.Report(
                        TemplatingDiagnosticDescriptors.MultipleAdviceAttributes.CreateRoslynDiagnostic(
                            declaredSymbol.GetDiagnosticLocation(),
                            (adviceAttributes[0].Member, adviceAttributes[0].AttributeClass, adviceAttributes[1].Member,
                             adviceAttributes[1].AttributeClass) ) );
                }

                var compilation = this._compilationContext.SourceCompilation;
                var reflectionMapper = this._compilationContext.ReflectionMapper;

                bool IsAspect( INamedTypeSymbol symbol )
                {
                    return compilation.HasImplicitConversion( symbol, reflectionMapper.GetTypeSymbol( typeof(IAspect) ) );
                }

                bool IsFabric( INamedTypeSymbol symbol )
                {
                    return compilation.HasImplicitConversion( symbol, reflectionMapper.GetTypeSymbol( typeof(Fabric) ) );
                }

                bool IsTemplateProvider( INamedTypeSymbol symbol )
                {
                    return compilation.HasImplicitConversion( symbol, reflectionMapper.GetTypeSymbol( typeof(ITemplateProvider) ) );
                }

                // Report an error for struct aspect.
                if ( declaredSymbol is INamedTypeSymbol { IsValueType: true } typeSymbol && IsAspect( typeSymbol ) )
                {
                    this.Report(
                        TemplatingDiagnosticDescriptors.AspectCantBeStruct.CreateRoslynDiagnostic(
                            declaredSymbol.GetDiagnosticLocation(),
                            declaredSymbol ) );
                }

                // Get the type scope.
                var typeScope = declaredSymbol is INamedTypeSymbol ? scope : this._currentTypeScope;

                // Get the template info.
                var templateInfo = this._currentTemplateInfo;

                if ( templateInfo == null || templateInfo.IsNone )
                {
                    templateInfo = this._classifier.GetTemplateInfo( declaredSymbol );
                }

                if ( !templateInfo.IsNone )
                {
                    if ( !IsSupportedTemplateDeclaration( declaredSymbol ) )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.CannotMarkDeclarationAsTemplate.CreateRoslynDiagnostic(
                                declaredSymbol.GetDiagnosticLocation(),
                                declaredSymbol ) );
                    }
                    else if ( declaredSymbol is IMethodSymbol { IsExtensionMethod: true } )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.ExtensionMethodTemplateNotSupported.CreateRoslynDiagnostic(
                                declaredSymbol.GetDiagnosticLocation(),
                                declaredSymbol ) );
                    }

                    var containingType = declaredSymbol.ContainingType;

                    if ( !IsAspect( containingType ) && !IsFabric( containingType ) && !IsTemplateProvider( containingType ) )
                    {
                        this.Report(
                            TemplatingDiagnosticDescriptors.TemplatesHaveToBeInTemplateProvider.CreateRoslynDiagnostic(
                                declaredSymbol.GetDiagnosticLocation(),
                                (declaredSymbol, containingType) ) );
                    }
                }

                // Report error on conflict scope.
                if ( scope == TemplatingScope.Conflict )
                {
                    this._classifier.ReportScopeError( node, declaredSymbol, this );
                }

                // Report error when we have compile-time code but no namespace import for fast detection.
                if ( scope != TemplatingScope.RunTimeOnly
                     && !this._hasCompileTimeCodeFast
                     && !SystemTypeDetector.IsSystemType( declaredSymbol.GetClosestContainingType()! ) )
                {
                    var attributeName = scope == TemplatingScope.RunTimeOrCompileTime ? nameof(RunTimeOrCompileTimeAttribute) : nameof(CompileTimeAttribute);

                    this.Report(
                        TemplatingDiagnosticDescriptors.CompileTimeCodeNeedsNamespaceImport.CreateRoslynDiagnostic(
                            node.GetDiagnosticLocation(),
                            (declaredSymbol, CompileTimeCodeFastDetector.Namespace, attributeName) ) );
                }

                // Check that 'dynamic' is used only in a template or in run-time-only code.
                if ( scope is TemplatingScope.Dynamic or TemplatingScope.DynamicTypeConstruction && typeScope != TemplatingScope.RunTimeOnly
                                                                                                 && templateInfo.IsNone )
                {
                    this.Report(
                        TemplatingDiagnosticDescriptors.OnlyNamedTemplatesCanHaveDynamicSignature.CreateRoslynDiagnostic(
                            declaredSymbol.GetDiagnosticLocation(),
                            (declaredSymbol, declaredSymbol.ContainingType!, typeScope!.Value) ) );
                }

                // Check that run-time members are contained in run-time types.
                if ( scope == TemplatingScope.RunTimeOnly && typeScope != TemplatingScope.RunTimeOnly && templateInfo.IsNone )
                {
                    // If we have an illegal run-time scope, we don't perform the scope transition, so we get error messages on the node contents.
                    return default;
                }

                // Assign the new context.
                var context = new Context( this, declaredSymbol );
                this._currentScope = scope;
                this._currentTypeScope = typeScope;
                this._currentDeclaration = declaredSymbol;
                this._currentTemplateInfo = templateInfo;

                return context;
            }

            private static bool IsSupportedTemplateDeclaration( ISymbol declaredSymbol )
                => declaredSymbol is not IMethodSymbol
                {
                    MethodKind: MethodKind.Constructor or MethodKind.Destructor or MethodKind.Conversion or MethodKind.UserDefinedOperator
                };

            private readonly struct Context : IDisposable
            {
                private readonly Visitor? _parent;
                private readonly TemplatingScope? _previousTypeScope;
                private readonly TemplatingScope? _previousScope;
                private readonly TemplateInfo? _previousTemplateInfo;
                private readonly ISymbol? _previousDeclaration;

                public Context( Visitor parent, ISymbol? declaredSymbol )
                {
                    this._parent = parent;
                    this._previousTypeScope = parent._currentTypeScope;
                    this._previousScope = parent._currentScope;
                    this._previousTemplateInfo = parent._currentTemplateInfo;
                    this._previousDeclaration = parent._currentDeclaration;
                    this.DeclaredSymbol = declaredSymbol;
                }

                public ISymbol? DeclaredSymbol { get; }

                public void Dispose()
                {
                    if ( this._parent != null )
                    {
                        this._parent._currentScope = this._previousScope;
                        this._parent._currentTemplateInfo = this._previousTemplateInfo;
                        this._parent._currentDeclaration = this._previousDeclaration;
                        this._parent._currentTypeScope = this._previousTypeScope;
                    }
                }
            }
        }
    }
}