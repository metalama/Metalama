// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using static Metalama.Framework.Engine.CompileTime.TemplatingScope;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

#pragma warning disable SA1124 // Don't use regions

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// A <see cref="CSharpSyntaxRewriter"/> that adds annotation that distinguish compile-time from
/// run-time syntax nodes. The input should be a syntax tree annotated with a <see cref="SyntaxTreeAnnotationMap"/>.
/// </summary>
internal sealed partial class TemplateAnnotator : SafeSyntaxRewriter, IDiagnosticAdder
{
    private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;
    private readonly IDiagnosticAdder _diagnosticAdder;
    private readonly SerializableTypes _serializableTypes;
    private readonly CancellationToken _cancellationToken;
    private readonly TemplateMemberClassifier _templateMemberClassifier;
    private readonly TypeParameterDetectionVisitor _typeParameterDetectionVisitor;
    private readonly TemplateProjectManifestBuilder? _templateProjectManifestBuilder;

    /// <summary>
    /// Scope of locally-defined symbols (local variables, anonymous types, lambda parameters, ....).
    /// </summary>
    private readonly Dictionary<ISymbol, TemplatingScope> _localScopes;

    private readonly ISymbolClassifier _symbolScopeClassifier;
    private readonly SafeSymbolComparer _symbolComparer;

    private ScopeContext _currentScopeContext;

    private ISymbol? _currentTemplateMember;
    private bool _isInLocalFunction;

    public TemplateAnnotator(
        ClassifyingCompilationContext compilationContext,
        SyntaxTreeAnnotationMap syntaxTreeAnnotationMap,
        IDiagnosticAdder diagnosticAdder,
        SerializableTypes serializableTypes,
        TemplateProjectManifestBuilder? templateProjectManifestBuilder,
        CancellationToken cancellationToken )
    {
        this._symbolScopeClassifier = compilationContext.SymbolClassifier;
        this._localScopes = new Dictionary<ISymbol, TemplatingScope>( compilationContext.CompilationContext.SymbolComparer );
        this._syntaxTreeAnnotationMap = syntaxTreeAnnotationMap;
        this._diagnosticAdder = diagnosticAdder;
        this._serializableTypes = serializableTypes;
        this._cancellationToken = cancellationToken;
        this._templateProjectManifestBuilder = templateProjectManifestBuilder;

        this._templateMemberClassifier = new TemplateMemberClassifier( compilationContext, syntaxTreeAnnotationMap );
        this._typeParameterDetectionVisitor = new TypeParameterDetectionVisitor( this );
        this._symbolComparer = compilationContext.CompilationContext.SymbolComparer;

        // add default values of scope
        this._currentScopeContext = ScopeContext.Default;
    }

    public bool Success { get; private set; } = true;

    /// <summary>
    /// Reports a diagnostic.
    /// </summary>
    /// <param name="descriptor">Diagnostic descriptor.</param>
    /// <param name="targetNode">Node on which the diagnostic should be reported.</param>
    /// <param name="arguments">Arguments of the formatting string.</param>
    private void ReportDiagnostic<T>( DiagnosticDefinition<T> descriptor, SyntaxNodeOrToken targetNode, T arguments )
        where T : notnull
    {
        var location = this._syntaxTreeAnnotationMap.GetLocation( targetNode );

        this.ReportDiagnostic( descriptor, location, arguments );
    }

    private void ReportDiagnostic<T>( DiagnosticDefinition<T> descriptor, Location? location, T arguments )
        where T : notnull
    {
        var diagnostic = descriptor.CreateRoslynDiagnostic( location, arguments );
        this._diagnosticAdder.Report( diagnostic );

        if ( diagnostic.Severity == DiagnosticSeverity.Error )
        {
            this.Success = false;
        }
    }

    void IDiagnosticAdder.Report( Diagnostic diagnostic )
    {
        this._diagnosticAdder.Report( diagnostic );

        if ( diagnostic.Severity == DiagnosticSeverity.Error )
        {
            this.Success = false;
        }
    }

    private void SetLocalSymbolScope( ISymbol symbol, TemplatingScope scope )
    {
        if ( scope != CompileTimeOnly && scope != RunTimeOnly )
        {
            throw new ArgumentOutOfRangeException( nameof(scope) );
        }

        if ( this._localScopes.ContainsKey( symbol ) )
        {
            throw new AssertionFailedException( $"The symbol {symbol} was already assigned to the scope {scope}." );
        }

        if ( scope == CompileTimeOnly )
        {
            if ( symbol is ILocalSymbol local )
            {
                this._currentScopeContext.RunTimeConditionalBlockVariables?.Add( local );
            }

            scope = this.FixCompileTimeReturningBothScopeWithSerializers( symbol );
        }

        this._localScopes.Add( symbol, scope );
    }

    private TemplatingScope FixCompileTimeReturningBothScopeWithSerializers( ISymbol symbol )
    {
        var valueType = symbol.GetExpressionType();

        if ( valueType != null )
        {
            var valueScope = this._symbolScopeClassifier.GetTemplatingScope( valueType );

            return valueScope switch
            {
                RunTimeOrCompileTime when this._serializableTypes.IsSerializable( valueType ) =>
                    CompileTimeOnlyReturningBoth,
                RunTimeOrCompileTime => CompileTimeOnly,
                CompileTimeOnly => CompileTimeOnly,

                // This should not happen in valid code., but we can have RuntimeOnly in case of invalid code.
                // In this case, an error is emitted.
                // However, the the continuation of the control flow, we fall back to this value anyway.
                _ => CompileTimeOnlyReturningRuntimeOnly
            };
        }

        return CompileTimeOnly;
    }

    /// <summary>
    /// Gets the scope of a symbol.
    /// </summary>
    /// <param name="symbol">A symbol.</param>
    /// <returns></returns>
    private TemplatingScope GetSymbolScope( ISymbol symbol )
    {
        switch ( symbol )
        {
            case IDiscardSymbol:
                return RunTimeOrCompileTime;

            // For local variables, we decide based on  _buildTimeLocals only. This collection is updated
            // at each iteration of the algorithm based on inferences from _requireMetaExpressionStack.
            case ILocalSymbol or INamedTypeSymbol { IsAnonymousType: true } or IParameterSymbol when this._localScopes.TryGetValue( symbol, out var scope ):
                return scope;

            // When a local variable is assigned to an anonymous type, the scope is unknown because the anonymous
            // type is visited after the variable identifier.
            case ILocalSymbol or INamedTypeSymbol { IsAnonymousType: true }:
                return LateBound;

            case { ContainingType: { IsAnonymousType: true } containingType }:
                return GetMoreSpecificScope( this.GetSymbolScope( containingType ) );

            // Template parameters are always evaluated at compile-time, but run-time template parameters return a run-time value.
            case IParameterSymbol templateParameter when TemplateMemberSymbolClassifier.IsTemplateParameter( templateParameter ):
                var parameterScope = this._symbolScopeClassifier.GetTemplatingScope( templateParameter );

                return parameterScope.GetExpressionExecutionScope() == CompileTimeOnly
                    ? parameterScope
                    : RunTimeTemplateParameter;

            // Template type parameters can be run-time or compile-time. If a template type parameter is not marked as compile-time, it is run-time (there is no scope-neutral).
            case ITypeParameterSymbol typeParameter when TemplateMemberSymbolClassifier.IsTemplateTypeParameter( typeParameter ):
                var typeParameterScope = this._symbolScopeClassifier.GetTemplatingScope( typeParameter );

                return typeParameterScope.GetExpressionExecutionScope() == CompileTimeOnly
                    ? typeParameterScope
                    : RunTimeOnly;

            case IMethodSymbol { MethodKind: MethodKind.LocalFunction }:
                return RunTimeOnly;

            case IMethodSymbol method when this._templateMemberClassifier.IsRunTimeMethod( method ):
                // The TemplateContext.runTime method must be processed separately. It is a compile-time-only method whose
                // return is run-time-only.

                return RunTimeOnly;
        }

        if ( symbol is IParameterSymbol )
        {
            // Local functions in templates are considered run-time-only, so their parameters are also run-time-only.
            if ( symbol.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } )
            {
                return RunTimeOnly;
            }

            // Remaining parameters should be parameters of expression lambdas, which are of unknown scope.
            return LateBound;
        }

        // Aspect members are processed as compile-time-only by the template compiler even if some members can also
        // be called from run-time code.
        if ( this.IsAspectMember( symbol ) )
        {
            var templateInfo = this._symbolScopeClassifier.GetTemplateInfo( symbol );

            if ( templateInfo.CanBeReferencedAsRunTimeCode )
            {
                return RunTimeOnly;
            }
            else
            {
                var valueScope = this._symbolScopeClassifier.GetTemplatingScope( symbol ).GetExpressionValueScope();

                return valueScope switch
                {
                    CompileTimeOnly => CompileTimeOnly,
                    RunTimeOrCompileTime => this.FixCompileTimeReturningBothScopeWithSerializers( symbol ),
                    RunTimeOnly => CompileTimeOnlyReturningRuntimeOnly,
                    _ => throw new AssertionFailedException( $"Unexpected templating scope: {valueScope}." )
                };
            }
        }

        // For other symbols, we use the SymbolScopeClassifier.
        var templatingScope = this._symbolScopeClassifier.GetTemplatingScope( symbol );

        return GetMoreSpecificScope( templatingScope );

        TemplatingScope GetMoreSpecificScope( TemplatingScope scope )
        {
            if ( scope == RunTimeOrCompileTime )
            {
                if ( this._currentScopeContext.PreferRunTimeExpression )
                {
                    if ( symbol is ITypeSymbol typeSymbol )
                    {
                        if ( !this._serializableTypes.IsSerializable( typeSymbol ) )
                        {
                            return RunTimeOnly;
                        }
                    }
                }

                return RunTimeOrCompileTime;
            }
            else if ( scope == CompileTimeOnlyReturningBoth )
            {
                return this.FixCompileTimeReturningBothScopeWithSerializers( symbol );
            }
            else
            {
                return scope;
            }
        }
    }

    /// <summary>
    /// Gets the common scope of many symbols (all candidates of for the same name), or null if
    /// there is no common scope.
    /// </summary>
    private TemplatingScope? GetCommonSymbolScope( IEnumerable<ISymbol> symbols )
    {
        var scope = default(TemplatingScope?);

        foreach ( var symbol in symbols )
        {
            var thisScope = this.GetSymbolScope( symbol );

            if ( scope == null )
            {
                scope = thisScope;
            }
            else if ( scope != thisScope )
            {
                return null;
            }
        }

        return scope;
    }

    /// <summary>
    /// Determines if a symbol is a member of the current template class (or aspect class).
    /// </summary>
    private bool IsAspectMember( ISymbol symbol )
        => this._currentTemplateMember != null
           && symbol.ContainingType != null
           && symbol.ContainingType.SpecialType != SpecialType.System_Object
           && this._symbolComparer.IsMemberOf( symbol, this._currentTemplateMember.ContainingType );

    private TemplatingScope[] GetNodeScopes( IReadOnlyCollection<SyntaxNode?> nodes )
    {
        var scopes = new TemplatingScope[nodes.Count];

        var i = 0;

        foreach ( var node in nodes )
        {
            scopes[i] = this.GetNodeScope( node );
            i++;
        }

        return scopes;
    }

    /// <summary>
    /// Gets the scope of a <see cref="SyntaxNode"/>.
    /// </summary>
    private TemplatingScope GetNodeScope( SyntaxNode? node ) => this.GetNodeScope( node, false );

    /// <summary>
    /// Gets the scope of a <see cref="SyntaxNode"/>.
    /// </summary>
    private TemplatingScope GetNodeScope( SyntaxNode? node, bool forAssignment )
    {
        if ( node == null )
        {
            return RunTimeOrCompileTime;
        }

        // If the node is dynamic, it is run-time only.
        if ( this._templateMemberClassifier.IsNodeOfDynamicType( node ) )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

            // this is to ensure that meta.ProceedAsync().ConfigureAwait(false) is classified as CompileTimeOnlyReturningRuntimeOnly,
            // so that it's treated like a Proceed expression
            if ( symbol.IsTaskConfigureAwait() && node is MemberAccessExpressionSyntax { Expression: var configuredExpression } )
            {
                symbol = this._syntaxTreeAnnotationMap.GetSymbol( configuredExpression );
            }

            // Dynamic local variables are considered compile-time because they must be transformed. 
            if ( !forAssignment && this._templateMemberClassifier.RequiresCompileTimeExecution( symbol ) )
            {
                return CompileTimeOnlyReturningRuntimeOnly;
            }
            else
            {
                return Dynamic;
            }
        }

        switch ( node )
        {
            case NameSyntax:
            case TupleTypeSyntax:
                // If the node is an identifier, it means it should have a symbol (or at least candidates)
                // and the scope is given by the symbol.
                // The same applies to tuples.
                return this.GetCommonSymbolScope( this._syntaxTreeAnnotationMap.GetCandidateSymbols( node ) )
                    .GetValueOrDefault( RunTimeOrCompileTime );

            case NullableTypeSyntax nullableType:
                return this.GetNodeScope( nullableType.ElementType );

            default:
                // Otherwise, the scope is given by the annotation given by the deeper
                // visitor or the previous algorithm iteration.
                return node.GetScopeFromAnnotation().GetValueOrDefault();
        }
    }

    private void ReportScopeError( SyntaxNode node )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node ).AssertSymbolNotNull();

        this._symbolScopeClassifier.ReportScopeError( node, symbol, this );
    }

    private TemplatingScope GetAssignmentScope( SyntaxNode node )
    {
        var scope = this.GetNodeScope( node, true );

        if ( scope == CompileTimeOnlyReturningBoth && node is TupleExpressionSyntax )
        {
            return CompileTimeOnly;
        }
        else
        {
            return scope.GetExpressionExecutionScope();
        }
    }

    // ReSharper disable once UnusedMember.Local

    private TemplatingScope GetExpressionTypeScope( ExpressionSyntax? node )
    {
        if ( node != null && this._syntaxTreeAnnotationMap.GetExpressionType( node ) is { } parentExpressionType )
        {
            return this.GetSymbolScope( parentExpressionType );
        }
        else
        {
            return RunTimeOrCompileTime;
        }
    }

    private TemplatingScope GetExpressionScope( IReadOnlyList<SyntaxNode?>? annotatedChildren, SyntaxNode originalParent, bool reportError = true )
    {
        if ( annotatedChildren == null || annotatedChildren.Count == 0 )
        {
            return RunTimeOrCompileTime;
        }

        var scopes = this.GetNodeScopes( annotatedChildren );

        return this.GetExpressionScope( annotatedChildren, scopes, originalParent, reportError );
    }

    /// <summary>
    /// Gives the <see cref="TemplatingScope"/> of a parent given the scope of its children.
    /// </summary>
    /// <param name="childrenScopes"></param>
    /// <param name="originalParent"></param>
    /// <returns></returns>
    private TemplatingScope GetExpressionScope(
        IReadOnlyList<SyntaxNode?> children,
        IReadOnlyList<TemplatingScope> childrenScopes,
        SyntaxNode originalParent,
        bool reportError = true,
        bool preferCompileTime = true )
    {
        // Get the scope of type of the parent node.

        var parentExpressionScope = RunTimeOrCompileTime;

        if ( originalParent is ExpressionSyntax originalExpression )
        {
            parentExpressionScope = this.GetExpressionTypeScope( originalExpression );
        }

        var combinedExecutionScope = RunTimeOrCompileTime;
        var combinedValueScope = RunTimeOrCompileTime;
        var useCompileTimeIfPossible = false;
        var lastNonNeutralNodeIndex = -1;

        for ( var i = 0; i < childrenScopes.Count; i++ )
        {
            var childScope = childrenScopes[i];
            var childExecutionScope = childScope.GetExpressionExecutionScope();
            var childValueScope = childScope.GetExpressionValueScope();

            if ( childExecutionScope == CompileTimeOnly )
            {
                // If a child executes at compile time, if we can, we prefer to evaluate the whole expression at compile time.
                useCompileTimeIfPossible = true;
            }

            combinedExecutionScope = combinedExecutionScope.GetCombinedExecutionScope( childExecutionScope );
            combinedValueScope = combinedValueScope.GetCombinedValueScope( childValueScope );

            if ( combinedExecutionScope == Conflict ||
                 combinedValueScope == Conflict )
            {
                if ( reportError )
                {
                    // Report an error.
                    if ( lastNonNeutralNodeIndex >= 0 )
                    {
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.ExpressionScopeConflictBecauseOfChildren,
                            originalParent,
                            (originalParent.ToString(), children[lastNonNeutralNodeIndex].AssertNotNull().ToString(),
                             childrenScopes[lastNonNeutralNodeIndex].ToDisplayString(),
                             children[i].AssertNotNull().ToString(), childrenScopes[i].ToDisplayString()) );
                    }
                    else
                    {
                        throw new AssertionFailedException( $"The expression '{children[i]}' seems to be in conflict with itself." );
                    }

                    // We don't propagate the conflict state after we report the error, because this would cause the reporting of more errors and be more confusing.
                    return RunTimeOrCompileTime;
                }
                else
                {
                    return Conflict;
                }
            }

            if ( childExecutionScope != RunTimeOrCompileTime )
            {
                lastNonNeutralNodeIndex = i;
            }
        }

        combinedExecutionScope = combinedExecutionScope.GetCombinedExecutionScope( parentExpressionScope.GetExpressionExecutionScope() );
        combinedValueScope = combinedValueScope.GetCombinedValueScope( parentExpressionScope.GetExpressionValueScope() );

        if ( combinedExecutionScope == Conflict ||
             combinedValueScope == Conflict )
        {
            if ( reportError )
            {
                // Report an error.
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.ExpressionScopeConflictBecauseOfParent,
                    originalParent,
                    (originalParent.ToString(), parentExpressionScope.ToDisplayString(), children[lastNonNeutralNodeIndex].AssertNotNull().ToString(),
                     childrenScopes[lastNonNeutralNodeIndex].ToDisplayString()) );

                // We don't propagate the conflict state after we report the error, because this would cause the reporting of more errors and be more confusing.
                return RunTimeOrCompileTime;
            }
            else
            {
                return Conflict;
            }
        }

        var resultingScope = (combinedExecutionScope, combinedValueScope) switch
        {
            (_, LateBound) => LateBound,
            (LateBound, _) => LateBound,
            (_, Conflict) => Conflict,
            (Conflict, _) => Conflict,
            (CompileTimeOnly, CompileTimeOnly) => CompileTimeOnly,
            (CompileTimeOnly, RunTimeOnly) => RunTimeOnly,
            (CompileTimeOnly, RunTimeOrCompileTime) => CompileTimeOnlyReturningBoth,
            (RunTimeOrCompileTime, RunTimeOrCompileTime) => RunTimeOrCompileTime,
            (RunTimeOnly, _) => RunTimeOnly,
            _ => throw new AssertionFailedException( $"Unexpected combination: ({combinedExecutionScope}, {combinedValueScope})." )
        };

        if ( resultingScope == RunTimeOrCompileTime && useCompileTimeIfPossible && preferCompileTime )
        {
            // Switch from RunTimeOrCompileTime to CompileTimeOnlyReturningBoth for serializable types, when preferred.
            if ( IsExpressionOfSerializableType() == true )
            {
                resultingScope = CompileTimeOnlyReturningBoth;
            }
        }
        else if ( resultingScope == CompileTimeOnlyReturningBoth )
        {
            // Switch from CompileTimeOnlyReturningBoth to RunTimeOrCompileTime for non-serializable types.
            if ( IsExpressionOfSerializableType() == false )
            {
                resultingScope = RunTimeOrCompileTime;
            }
        }

        return resultingScope;

        bool? IsExpressionOfSerializableType()
        {
            if ( originalParent is ExpressionSyntax expression &&
                 this._syntaxTreeAnnotationMap.GetExpressionType( expression ) is { } expressionType )
            {
                return this._serializableTypes.IsSerializable( expressionType );
            }

            return null;
        }
    }

    private ScopeContextCookie WithScopeContext( ScopeContext? scopeContext )
    {
        if ( scopeContext == this._currentScopeContext || scopeContext == null )
        {
            return default;
        }

        var cookie = new ScopeContextCookie( this, this._currentScopeContext );
        this._currentScopeContext = scopeContext;

        return cookie;
    }

    [return: NotNullIfNotNull( "node" )]
    private T? Visit<T>( T? node )
        where T : SyntaxNode
        => (T?) this.DefaultVisitImpl( node );

    /// <summary>
    /// Default visitor.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    protected override SyntaxNode? VisitCore( SyntaxNode? node ) => this.DefaultVisitImpl( node );

    [return: NotNullIfNotNull( "node" )]
    private SyntaxNode? DefaultVisitImpl( SyntaxNode? node )
    {
        if ( node == null )
        {
            return null;
        }

        this._cancellationToken.ThrowIfCancellationRequested();

        // Adds annotations to the children node.
        var transformedNode = base.VisitCore( node )!;

        return this.AddScopeAnnotationToVisitedNode( node, transformedNode );
    }

    /// <summary>
    /// Adds scope annotation to a node that has been visited - so children are annotated but not the node itself.
    /// </summary>
    private SyntaxNode AddScopeAnnotationToVisitedNode( SyntaxNode node, SyntaxNode visitedNode )
    {
        if ( this._currentScopeContext.ForceCompileTimeOnlyExpression )
        {
            var currentScope = visitedNode.GetScopeFromAnnotation();

            if ( currentScope?.GetExpressionExecutionScope() is RunTimeOnly ||
                 this._templateMemberClassifier.IsNodeOfDynamicType( visitedNode ) )
            {
                // The current expression is obliged to be compile-time-only by inference.
                // Emit an error if the type of the expression is inferred to be runtime-only.
                this.RequireScope(
                    visitedNode,
                    RunTimeOnly,
                    CompileTimeOnly,
                    this._currentScopeContext.PreferredScopeReason! );

                return visitedNode.AddScopeMismatchAnnotation();
            }

            // the current expression can be annotated as unknown (f.e. parameters of lambda expression)
            // that means it can be used as compile time and it doesn't need to be annotated as compileTime.
            if ( currentScope != LateBound && currentScope != TypeOfRunTimeType )
            {
                return visitedNode.ReplaceScopeAnnotation( CompileTimeOnly );
            }
        }

        if ( visitedNode.HasScopeAnnotation() || visitedNode is StatementSyntax )
        {
            // If the transformed node has already an annotation, it means it has already been classified by
            // a previous run of the algorithm, and there is no need to classify it again.
            return visitedNode;
        }

        // Here is the default implementation for expressions. The scope of the parent is the combined scope of the children.
        var childNodes = visitedNode.ChildNodes().Where( n => n is ExpressionSyntax or InterpolationSyntax );

        var combinedScope = this.GetExpressionScope( childNodes.ToReadOnlyList(), node );

        return visitedNode.AddScopeAnnotation( combinedScope );
    }

    #region Anonymous objects

    public override SyntaxNode VisitAnonymousObjectMemberDeclarator( AnonymousObjectMemberDeclaratorSyntax node )
    {
        var scope = this._currentScopeContext.ForceCompileTimeOnlyExpression ? CompileTimeOnly : RunTimeOnly;

        return node.Update( node.NameEquals, this.Visit( node.Expression ) ).AddScopeAnnotation( scope );
    }

    public override SyntaxNode VisitAnonymousObjectCreationExpression( AnonymousObjectCreationExpressionSyntax node )
    {
        var scope = this._currentScopeContext.ForceCompileTimeOnlyExpression ? CompileTimeOnly : RunTimeOnly;

        var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

        if ( symbol != null )
        {
            var existingScope = this.GetSymbolScope( symbol );

            if ( existingScope == LateBound )
            {
                this.SetLocalSymbolScope( symbol, scope );
            }
            else if ( existingScope != scope )
            {
                this.ReportDiagnostic( TemplatingDiagnosticDescriptors.AnonymousTypeDifferentScopes, node, symbol );
            }
        }

        // Anonymous objects are currently run-time-only unless they are in a compile-time-only scope -- until we implement more complex rules.
        var transformedMembers =
            node.Initializers.SelectAsReadOnlyList( i => this.Visit( i ).AddScopeAnnotation( scope ) );

        return node.Update(
                node.NewKeyword,
                node.OpenBraceToken,
                SeparatedList( transformedMembers, node.Initializers.GetSeparators() ),
                node.CloseBraceToken )
            .AddScopeAnnotation( scope );
    }

    #endregion

    public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitClassDeclaration( n ) );

    public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node )
        => this.VisitTypeDeclaration( node, n => base.VisitStructDeclaration( n ) );

    public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node )
        => this.VisitTypeDeclaration( node, n => base.VisitRecordDeclaration( n ) );

    public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node )
        => this.VisitTypeDeclaration( node, n => base.VisitDelegateDeclaration( n ) );

    public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitEnumDeclaration( n ) );

    private T VisitTypeDeclaration<T>( T node, Func<T, SyntaxNode?> callBase )
        where T : SyntaxNode
    {
        var typeScope = this.GetSymbolScope( this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node ).AssertSymbolNotNull() );

        if ( typeScope == Conflict )
        {
            return node;
        }
        else if ( typeScope != RunTimeOnly )
        {
            return ((T) callBase( node )!).AddScopeAnnotation( typeScope );
        }
        else
        {
            // This is not a build-time type so there's no need to analyze it.
            // The scope annotation is needed for syntax highlighting.
            return node.AddScopeAnnotation( typeScope );
        }
    }

    public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
    {
        if ( this._currentScopeContext.IsDynamicTypingForbidden && node.Identifier.Text == "dynamic" )
        {
            this.ReportDiagnostic( TemplatingDiagnosticDescriptors.CannotUseDynamicTypingInLocalFunction, node.GetDiagnosticLocation(), default );
        }

        var identifierNameSyntax = (IdentifierNameSyntax) base.VisitIdentifierName( node )!;
        var symbols = this._syntaxTreeAnnotationMap.GetCandidateSymbols( node ).ToReadOnlyList();
        var scope = this.GetCommonSymbolScope( symbols );

        if ( scope is null or DynamicTypeConstruction )
        {
            // An error should be emitted elsewhere, so we continue considering it is run-time.
            scope = RunTimeOrCompileTime;
        }
        else if ( scope == Conflict )
        {
            this._symbolScopeClassifier.ReportScopeError( node, symbols.First(), this );

            scope = RunTimeOrCompileTime;
        }

        if ( scope.Value.GetExpressionExecutionScope() == CompileTimeOnly )
        {
            var symbol = symbols[0];

            if ( this._symbolScopeClassifier.GetTemplateInfo( symbol ).AttributeType == TemplateAttributeType.Template )
            {
                if ( symbol is not IMethodSymbol )
                {
                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.OnlyMethodsCanBeSubtemplates,
                        node,
                        (symbol, this._currentTemplateMember!) );
                }
                else if ( node.Parent is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax }) )
                {
                    // Only name(...) and expr.name(...) are valid ways to reference a subtemplate.

                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.SubtemplatesHaveToBeInvoked,
                        node,
                        symbol );
                }
            }
        }

        var annotatedNode = identifierNameSyntax.AddScopeAnnotation( scope );

        annotatedNode = (IdentifierNameSyntax) this.AddColoringAnnotations( annotatedNode, symbols.FirstOrDefault(), scope.Value )!;

        return annotatedNode;
    }

    private SyntaxNodeOrToken AddColoringAnnotations( SyntaxNodeOrToken nodeOrToken, ISymbol? symbol, TemplatingScope scope )
    {
        switch ( symbol )
        {
            case null:
                // Coverage: ignore.
                return nodeOrToken;

            case ILocalSymbol when scope.GetExpressionExecutionScope() == CompileTimeOnly:
                nodeOrToken = nodeOrToken.AddColoringAnnotation( TextSpanClassification.CompileTimeVariable );

                break;

            default:
                {
                    if ( TemplateMemberSymbolClassifier.HasTemplateKeywordAttribute( symbol ) )
                    {
                        nodeOrToken = nodeOrToken.AddColoringAnnotation( TextSpanClassification.TemplateKeyword );
                    }
                    else
                    {
                        var node = nodeOrToken.AsNode() ?? nodeOrToken.Parent;

                        if ( node != null &&
                             symbol is not (ITypeSymbol or ILocalSymbol) &&
                             this._templateMemberClassifier.IsNodeOfDynamicType( node ) )
                        {
                            // Annotate dynamic members differently for syntax coloring.
                            nodeOrToken = nodeOrToken.AddColoringAnnotation( TextSpanClassification.Dynamic );
                        }
                    }

                    break;
                }
        }

        return nodeOrToken;
    }

    public override SyntaxNode VisitMemberBindingExpression( MemberBindingExpressionSyntax node )
    {
        var transformedName = this.Visit( node.Name );

        return node.WithName( transformedName ).WithScopeAnnotationFrom( transformedName );
    }

    public override SyntaxNode VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
    {
        this.VisitAccessExpressionCore(
            node.Expression,
            node.Name,
            node.OperatorToken,
            out var transformedExpression,
            out var transformedName,
            out var transformedOperator,
            out var scope );

        if ( this._syntaxTreeAnnotationMap.GetSymbol( transformedExpression ) is ITypeParameterSymbol typeParameter
             && this.GetNodeScope( transformedExpression ) == CompileTimeOnlyReturningRuntimeOnly )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.StaticInterfaceMembersNotSupportedOnCompileTimeTemplateTypeParameters,
                node,
                (transformedName.ToString(), typeParameter) );
        }

        var transformedNode = node
            .Update( transformedExpression, transformedOperator, (SimpleNameSyntax) transformedName )
            .AddScopeAnnotation( scope );

        return transformedNode;
    }

    public override SyntaxNode VisitConditionalAccessExpression( ConditionalAccessExpressionSyntax node )
    {
        this.VisitAccessExpressionCore(
            node.Expression,
            node.WhenNotNull,
            node.OperatorToken,
            out var transformedExpression,
            out var transformedWhenNotNull,
            out var transformedOperator,
            out var scope );

        if ( scope == CompileTimeOnlyReturningRuntimeOnly && this.GetNodeScope( transformedExpression ) == CompileTimeOnly )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.ExpressionScopeConflictInConditionalAccess,
                node,
                (node.ToString(), node.Expression.ToString(), node.Expression + node.WhenNotNull.ToString()) );
        }

        var transformedNode = node
            .Update( transformedExpression, transformedOperator, transformedWhenNotNull )
            .AddScopeAnnotation( scope );

        return transformedNode;
    }

    private void VisitAccessExpressionCore(
        ExpressionSyntax left,
        ExpressionSyntax right,
        SyntaxToken operatorToken,
        out ExpressionSyntax transformedLeft,
        out ExpressionSyntax transformedRight,
        out SyntaxToken transformedOperatorToken,
        out TemplatingScope scope )
    {
        transformedRight = this.Visit( right );

        var rightScope = this.GetNodeScope( transformedRight );

        ScopeContext? context = null;
        scope = RunTimeOrCompileTime;

        switch ( rightScope )
        {
            case CompileTimeOnly:
            case CompileTimeOnlyReturningBoth:
            case CompileTimeOnlyReturningRuntimeOnly:
                // If the member is compile-time (because of rules on the symbol), the expression on the left MUST be compile-time.
                context = this._currentScopeContext.CompileTimeOnly( $"a compile-time-only member '{right}'" );
                scope = rightScope;

                break;

            case Dynamic:
                // A member is run-time dynamic because the left part is dynamic, so there is no need to force it run-time.
                // It can actually contain build-time subexpressions.
                scope = Dynamic;

                break;

            case LateBound when this._syntaxTreeAnnotationMap.GetExpressionType( left ) is IDynamicTypeSymbol:
                // This is a member access of a dynamic receiver.
                scope = RunTimeOnly;
                context = this._currentScopeContext.RunTimePreferred( $"a member of the run-time-only '{right}'" );

                break;

            case RunTimeOnly:
                scope = RunTimeOnly;

                break;
        }

        using ( this.WithScopeContext( context ) )
        {
            transformedLeft = this.Visit( left );
        }

        var leftScope = this.GetNodeScope( transformedLeft );

        if ( scope == RunTimeOrCompileTime )
        {
            if ( rightScope == Conflict )
            {
                // Conflicts are ignored. They should be reported elsewhere.
                scope = leftScope;
            }
            else
            {
                Invariant.Assert( rightScope == RunTimeOrCompileTime );

                if ( leftScope is CompileTimeOnlyReturningRuntimeOnly )
                {
                    // Special case: we need to force run-time-only scope here.
                    scope = RunTimeOnly;
                }
                else
                {
                    scope = leftScope.GetExpressionExecutionScope() switch
                    {
                        RunTimeOnly => RunTimeOnly,
                        CompileTimeOnly => CompileTimeOnlyReturningBoth,
                        RunTimeOrCompileTime => RunTimeOrCompileTime,
                        LateBound => RunTimeOrCompileTime,
                        _ => throw new AssertionFailedException( $"Invalid combination: {leftScope}, {rightScope}." )
                    };
                }
            }
        }

        // If both sides of the member are template keywords, display the . as a template keyword too.
        transformedOperatorToken = operatorToken;

        if ( transformedLeft.GetColorFromAnnotation() == TextSpanClassification.TemplateKeyword &&
             transformedRight.GetColorFromAnnotation() == TextSpanClassification.TemplateKeyword )
        {
            transformedOperatorToken = transformedOperatorToken.AddColoringAnnotation( TextSpanClassification.TemplateKeyword );
        }
    }

    public override SyntaxNode VisitElementAccessExpression( ElementAccessExpressionSyntax node )
    {
        // In an element access (such as Tags[x]), the scope is given by the expression.

        var transformedExpression = this.Visit( node.Expression );
        var scope = this.GetNodeScope( transformedExpression );

        ScopeContext? context;

        if ( scope is CompileTimeOnly or CompileTimeOnlyReturningBoth )
        {
            context = this._currentScopeContext.CompileTimeOnly( $"element of the compile-time collection '{node.Expression}'" );
        }
        else if ( scope.IsCompileTimeMemberReturningRunTimeValue() )
        {
            scope = Dynamic;

            context = this._currentScopeContext.RunTimePreferred( $"element of the run-time-only collection '{node.Expression}'" );
        }
        else
        {
            context = null;
        }

        BracketedArgumentListSyntax transformedArguments;

        using ( this.WithScopeContext( context ) )
        {
            transformedArguments = this.Visit( node.ArgumentList );
        }

        return node.Update( transformedExpression, transformedArguments ).AddScopeAnnotation( scope );
    }

    public override SyntaxNode VisitInvocationExpression( InvocationExpressionSyntax node )
    {
        // nameof() is always compile-time.
        if ( node.IsNameOf() )
        {
            return node.AddScopeAnnotation( RunTimeOrCompileTime );
        }

        // If we have any out/ref argument that assigns a compile-time variable, the whole method call is compile-time, and we cannot
        // be in a run-time-conditional block.
        var compileTimeOutArguments = node.ArgumentList.Arguments.Where(
                a => a.RefKindKeyword.Kind() is SyntaxKind.OutKeyword or SyntaxKind.RefKeyword
                     && this.GetNodeScope( a.Expression ).GetExpressionExecutionScope() == CompileTimeOnly )
            .ToReadOnlyList();

        ScopeContext? expressionContext = null;

        foreach ( var compileTimeOutArgument in compileTimeOutArguments )
        {
            expressionContext =
                this._currentScopeContext.CompileTimeOnly( $"a call to a method that sets the compile-time expression '{compileTimeOutArgument}'" );

            this.CheckForMutatingCompileTimeExpressionInRunTimeConditionalBlock( compileTimeOutArgument.Expression );
        }

        // Transform the expression.
        ExpressionSyntax transformedExpression;

        using ( this.WithScopeContext( expressionContext ) )
        {
            transformedExpression = this.Visit( node.Expression );
        }

        var expressionScope = this.GetNodeScope( transformedExpression );

        var symbol = this._syntaxTreeAnnotationMap.GetInvocableSymbol( node.Expression );

        var templateInfo = symbol == null ? TemplateInfo.None : this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol );

        if ( templateInfo.CanBeReferencedAsSubtemplate )
        {
            if ( node.Parent is not ExpressionStatementSyntax )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.SubtemplateCallCantBeSubexpression,
                    node,
                    node.ToString() );
            }

            if ( (symbol!.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
                 && !symbol.IsSealed
                 && symbol is IMethodSymbol { Parameters: var parameters }
                 && parameters.Length > node.ArgumentList.Arguments.Count )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.SubtemplateCallWithMissingArgumentsCantBeVirtual,
                    node,
                    node.ToString() );
            }

            if ( symbol is IMethodSymbol { TypeParameters: var typeParameters }
                 && typeParameters.Any( tp => this.GetSymbolScope( tp ) == RunTimeOnly ) )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.SubtemplateCantHaveRunTimeTypeParameter,
                    node,
                    symbol );
            }

            // TODO: only forbid this when either calling the abstract template using base, or when there is no override.
            if ( templateInfo.IsAbstract )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.CantCallAbstractSubtemplate,
                    node,
                    symbol );
            }
        }

        InvocationExpressionSyntax updatedInvocation;

        if ( expressionScope.GetExpressionExecutionScope() != RunTimeOrCompileTime )
        {
            // If the scope of the expression on the left side is known (because of rules on the symbol),
            // we know the scope of arguments upfront. Otherwise, we need to decide of the invocation scope based on arguments (else branch of this if).

            var transformedArguments = new List<ArgumentSyntax>( node.ArgumentList.Arguments.Count );

            for ( var argumentIndex = 0; argumentIndex < node.ArgumentList.Arguments.Count; argumentIndex++ )
            {
                var argument = node.ArgumentList.Arguments[argumentIndex];

                var parameter = this._syntaxTreeAnnotationMap.GetParameterSymbol( argument );

                ExpressionSyntax transformedArgumentExpression;

                // Transform the argument value.
                var isDynamicParameter = TemplateMemberSymbolClassifier.IsDynamicParameter( parameter?.Type );

                var isRunTimeParameterOfSubtemplate =
                    parameter != null &&
                    templateInfo.CanBeReferencedAsSubtemplate &&
                    this._templateMemberClassifier.SymbolClassifier.GetTemplatingScope( parameter ).GetExpressionExecutionScope()
                    != CompileTimeOnly;

                if ( expressionScope.IsCompileTimeMemberReturningRunTimeValue() || isDynamicParameter )
                {
                    // dynamic or dynamic[]

                    using ( this.WithScopeContext(
                               this._currentScopeContext.RunTimePreferred(
                                   $"argument of the dynamic parameter '{parameter?.Name ?? argumentIndex.ToString( CultureInfo.InvariantCulture )}'" ) ) )
                    {
                        transformedArgumentExpression = this.Visit( argument.Expression );
                    }

                    // Dynamic arguments passed to a a compile-time method returning a compile-time value are forbidden.
                    // They must be explicitly cast to IExpression. It does not apply if the expression is used as a statement expression.
                    if ( isDynamicParameter && expressionScope == CompileTimeOnly && !node.Parent.IsKind( SyntaxKind.ExpressionStatement ) )
                    {
                        var argumentType = this._syntaxTreeAnnotationMap.GetExpressionType( argument.Expression );

                        if ( argumentType?.TypeKind is TypeKind.Dynamic )
                        {
                            this.ReportDiagnostic(
                                TemplatingDiagnosticDescriptors.DynamicArgumentMustBeCastToIExpression,
                                argument.Expression,
                                argument.Expression.ToString() );
                        }
                    }
                }
                else if ( isRunTimeParameterOfSubtemplate )
                {
                    using ( this.WithScopeContext(
                               this._currentScopeContext.RunTimePreferred( $"argument of the run-time parameter '{parameter!.Name}' of a called template" ) ) )
                    {
                        transformedArgumentExpression = this.Visit( argument.Expression ).AddTargetScopeAnnotation( RunTimeOnly );
                    }
                }
                else if ( expressionScope.EvaluatesToRunTimeValue() )
                {
                    using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( $"argument of the run-time method '{node.Expression}'" ) ) )
                    {
                        transformedArgumentExpression = this.Visit( argument.Expression );
                    }

                    var argumentType = this._syntaxTreeAnnotationMap.GetExpressionType( argument.Expression );

                    if ( argumentType != null && this._symbolScopeClassifier.GetTemplatingScope( argumentType ) == CompileTimeOnly )
                    {
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.CompileTimeTypeInInvocationOfRuntimeMethod,
                            argument.Expression,
                            (argumentType, node.Expression.ToString()) );
                    }
                }
                else
                {
                    using ( this.WithScopeContext( this._currentScopeContext.CompileTimeOnly( $"a compile-time expression '{node.Expression}'" ) ) )
                    {
                        transformedArgumentExpression = this.Visit( argument.Expression );
                    }
                }

                if ( isDynamicParameter )
                {
                    // Make sure that RunTimeOrCompileTime is interpreted as run-time, so we get the meta-expression we need.
                    transformedArgumentExpression = transformedArgumentExpression
                        .AddTargetScopeAnnotation( RunTimeOnly );
                }

                // The scope of the argument itself copies the scope of the method.
                var transformedArgument = argument.WithExpression( transformedArgumentExpression )
                    .WithTriviaFrom( argument )
                    .AddScopeAnnotation( expressionScope );

                if ( isDynamicParameter )
                {
                    transformedArgument = transformedArgument.AddTargetRequiresUserExpressionAnnotation();
                }

                transformedArguments.Add( transformedArgument );
            }

            updatedInvocation = node.Update(
                transformedExpression,
                ArgumentList(
                    node.ArgumentList.OpenParenToken,
                    SeparatedList( transformedArguments, node.ArgumentList.Arguments.GetSeparators() ),
                    node.ArgumentList.CloseParenToken ) );

            updatedInvocation = updatedInvocation.AddScopeAnnotation( expressionScope );

            if ( expressionScope == CompileTimeOnlyReturningRuntimeOnly && this._syntaxTreeAnnotationMap.GetExpressionType( node ) is
                    { TypeKind: TypeKind.Dynamic } )
            {
                updatedInvocation = updatedInvocation.AddTargetRequiresUserExpressionAnnotation();
            }
        }
        else
        {
            // If the expression on the left of the parenthesis is not compile-time,
            // we cannot take a decision on the parent expression.

            TemplatingScope invocationScope;

            var transformedArgumentList = (ArgumentListSyntax) this.VisitArgumentList( node.ArgumentList )!;
            transformedArgumentList = transformedArgumentList.WithOpenParenToken( node.ArgumentList.OpenParenToken );
            transformedArgumentList = transformedArgumentList.WithCloseParenToken( node.ArgumentList.CloseParenToken );
            updatedInvocation = node.Update( transformedExpression, transformedArgumentList );

            if ( expressionScope == RunTimeOnly || this._templateMemberClassifier.IsRunTimeMethod( node.Expression ) )
            {
                invocationScope = RunTimeOnly;
            }
            else
            {
                var children = new List<SyntaxNode>( transformedArgumentList.Arguments.Count + 1 ) { transformedExpression };

                children.AddRange( transformedArgumentList.Arguments );

                var childScopes = this.GetNodeScopes( children );

                // We do not prefer compile-time when the invocation is a statement because this is most
                // likely a run-time statement invocation. All compile-time methods that can be used as statements are explicitly marked.
                var preferCompileTime = !node.Parent.IsKind( SyntaxKind.ExpressionStatement );

                invocationScope = this.GetExpressionScope( children, childScopes, node, preferCompileTime: preferCompileTime );
            }

            updatedInvocation = updatedInvocation.AddScopeAnnotation( invocationScope );
        }

        // To make sure the expression `meta.RunTime( compileTimeExpression )` is correctly highlighted, the parentheses need to be explicitly colored as compile-time.
        if ( updatedInvocation.Expression is MemberAccessExpressionSyntax { Name: var invokedMemberName }
             && invokedMemberName.GetColorFromAnnotation() == TextSpanClassification.TemplateKeyword
             && updatedInvocation.ArgumentList.Arguments.All(
                 arg => arg.Expression.GetScopeFromAnnotation()?.GetExpressionExecutionScope() == CompileTimeOnly ) )
        {
            updatedInvocation = updatedInvocation.ReplaceTokens(
                [updatedInvocation.ArgumentList.OpenParenToken, updatedInvocation.ArgumentList.CloseParenToken],
                ( token, _ ) => token.AddColoringAnnotation( TextSpanClassification.CompileTime ) );

            // Note: if there is ever a non-compile-time TemplateKeyword that has multiple arguments, ArgumentList commas might need coloring too.
        }

        return updatedInvocation;
    }

    public override SyntaxNode VisitArgument( ArgumentSyntax node )
    {
        // We don't add an annotation to the argument because it needs to be inherited from the parent.
        var transformedExpression = this.Visit( node.Expression );

        return node.WithExpression( transformedExpression );
    }

    public override SyntaxNode VisitIfStatement( IfStatementSyntax node )
    {
        var annotatedCondition = this.Visit( node.Condition );
        var conditionScope = this.GetNodeScope( annotatedCondition );

        TemplatingScope ifScope;
        StatementSyntax annotatedStatement;
        StatementSyntax? annotatedElseStatement;
        ScopeContext? scopeContext;

        if ( conditionScope.GetExpressionValueScope( true ) is CompileTimeOnly or RunTimeOrCompileTime )
        {
            // We have an if statement where the condition is a compile-time expression. Add annotations
            // to the if and else statements but not to the blocks themselves.

            scopeContext = null;
            ifScope = CompileTimeOnly;

            annotatedCondition = annotatedCondition.ReplaceScopeAnnotationIfUndetermined( CompileTimeOnly );
        }
        else
        {
            // We have an if statement where the condition is a runtime expression. Any variable assignment
            // within this statement should make the variable as runtime-only, so we're calling EnterRuntimeConditionalBlock.

            scopeContext = this._currentScopeContext.RunTimeConditional( "if ( " + node.Condition + " )" );
            ifScope = RunTimeOnly;
        }

        using ( this.WithScopeContext( scopeContext ) )
        {
            // Statements of a compile-time control block must have an explicitly-set scope otherwise the template compiler
            // will look at the scope in the parent node, which is incorrect here.
            annotatedStatement = this.Visit( node.Statement ).ReplaceScopeAnnotationIfUndetermined( ifScope );
            annotatedElseStatement = this.Visit( node.Else?.Statement )?.ReplaceScopeAnnotationIfUndetermined( ifScope );
        }

        return node.Update(
                node.AttributeLists,
                node.IfKeyword,
                node.OpenParenToken,
                annotatedCondition,
                node.CloseParenToken,
                annotatedStatement,
                node.Else?.Update( node.Else.ElseKeyword, annotatedElseStatement! ).AddScopeAnnotation( ifScope ) )
            .AddScopeAnnotation( ifScope );
    }

    public override SyntaxNode VisitBreakStatement( BreakStatementSyntax node )
        => node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );

    public override SyntaxNode VisitContinueStatement( ContinueStatementSyntax node )
        => node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );

    public override SyntaxNode VisitForEachStatement( ForEachStatementSyntax node )
    {
        var local = (ILocalSymbol) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node )!;

        var annotatedExpression = this.Visit( node.Expression );

        if ( this._templateMemberClassifier.IsNodeOfDynamicType( node.Type ) &&
             !this._templateMemberClassifier.IsNodeOfDynamicType( annotatedExpression ) )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.DynamicVariableSetToNonDynamic,
                node.Identifier,
                node.Identifier.Text );
        }

        TemplatingScope forEachScope;
        string reason;

        if ( node.AwaitKeyword.IsKind( SyntaxKind.None ) )
        {
            forEachScope = this.GetNodeScope( annotatedExpression )
                .GetExpressionValueScope( true )
                .ReplaceIndeterminate( RunTimeOnly );

            reason = $"foreach ( {node.Type} {node.Identifier} in ... )";
        }
        else
        {
            forEachScope = RunTimeOnly;
            reason = $"await foreach ( {node.Type} {node.Identifier} in ... )";
        }

        annotatedExpression = annotatedExpression.ReplaceScopeAnnotationIfUndetermined( forEachScope );

        this.SetLocalSymbolScope( local, forEachScope );

        StatementSyntax annotatedStatement;

        using ( this.WithScopeContext( this._currentScopeContext.BreakOrContinue( forEachScope, reason ) ) )
        {
            // Statements of a compile-time control block must have an explicitly-set scope otherwise the template compiler
            // will look at the scope in the parent node, which is incorrect here.
            annotatedStatement = this.Visit( node.Statement ).ReplaceScopeAnnotationIfUndetermined( forEachScope );
        }

        var identifierClassification = forEachScope == CompileTimeOnly ? TextSpanClassification.CompileTimeVariable : default;

        var transformedNode =
            ForEachStatement(
                    node.AwaitKeyword,
                    node.ForEachKeyword,
                    node.OpenParenToken,
                    node.Type.AddTargetScopeAnnotation( forEachScope ),
                    node.Identifier.AddColoringAnnotation( identifierClassification ),
                    node.InKeyword,
                    annotatedExpression,
                    node.CloseParenToken,
                    annotatedStatement.AddTargetScopeAnnotation( forEachScope ) )
                .AddScopeAnnotation( forEachScope )
                .WithSymbolAnnotationsFrom( node );

        return transformedNode;
    }

    #region Pattern Matching

    public override SyntaxNode VisitDeclarationPattern( DeclarationPatternSyntax node )
    {
        // If the type of a pattern is compile-time-only, the variable is compile-time.

        var transformedType = this.Visit( node.Type );
        var scope = this.GetNodeScope( transformedType );

        var typeSymbol = this._syntaxTreeAnnotationMap.GetExpressionType( node.Type )!;

        var context = scope == CompileTimeOnly
            ? this._currentScopeContext.CompileTimeOnly( $"local variable of compile-time type '{typeSymbol}'" )
            : null;

        VariableDesignationSyntax transformedDesignation;

        using ( this.WithScopeContext( context ) )
        {
            transformedDesignation = this.Visit( node.Designation );
        }

        return node.Update( transformedType, transformedDesignation ).AddScopeAnnotation( scope );
    }

    public override SyntaxNode VisitIsPatternExpression( IsPatternExpressionSyntax node )
    {
        // The scope of a pattern expression is given by the expression (left part).
        var transformedExpression = this.Visit( node.Expression );
        var scope = this.GetNodeScope( transformedExpression ).GetExpressionValueScope();

        var context = scope == CompileTimeOnly
            ? this._currentScopeContext.CompileTimeOnly( $"pattern on the compile-time expression '{node.Expression}'" )
            : null;

        PatternSyntax transformedPattern;

        using ( this.WithScopeContext( context ) )
        {
            transformedPattern = this.Visit( node.Pattern );
        }

        if ( scope == RunTimeOnly )
        {
            this.RequireScope( transformedPattern, RunTimeOnly, $"pattern on the run-time expression '{node.Expression}'" );
        }

        return node.Update( transformedExpression, node.IsKeyword, transformedPattern ).AddScopeAnnotation( scope );
    }

    #endregion

    #region Variables

    public override SyntaxNode VisitSingleVariableDesignation( SingleVariableDesignationSyntax node )
    {
        var symbol = (ILocalSymbol?) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

        var scope = this._currentScopeContext.ForceCompileTimeOnlyExpression ? CompileTimeOnly : RunTimeOnly;

        var color = TextSpanClassification.Default;

        if ( symbol != null )
        {
            this.SetLocalSymbolScope( symbol, scope );

            if ( scope == CompileTimeOnly )
            {
                color = TextSpanClassification.CompileTimeVariable;
            }
        }

        var coloredIdentifier = node.Identifier.AddColoringAnnotation( color );
        var transformedNode = node.WithIdentifier( coloredIdentifier ).AddScopeAnnotation( scope );

        return transformedNode;
    }

    public override SyntaxNode VisitDeclarationExpression( DeclarationExpressionSyntax node )
    {
        // This methods processes in-line variable declarations expressions, like in `out var x`.

        var transformedType = this.Visit( node.Type );

        ScopeContext? context = null;

        if ( !this._currentScopeContext.ForceCompileTimeOnlyExpression && this.GetNodeScope( transformedType ) == CompileTimeOnly )
        {
            context = this._currentScopeContext.CompileTimeOnly( $"an inline variable declaration of compile-time type '{transformedType}" );
        }

        VariableDesignationSyntax transformedDesignation;

        using ( this.WithScopeContext( context ) )
        {
            transformedDesignation = this.Visit( node.Designation );
        }

        var scope = this.GetExpressionScope( [transformedType, transformedDesignation], node );

        return node.Update( transformedType, transformedDesignation ).AddScopeAnnotation( scope );
    }

    public override SyntaxNode VisitVariableDeclarator( VariableDeclaratorSyntax node )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node )!;

        if ( symbol is not ILocalSymbol local )
        {
            // It's a field, or a field-like event.

            // Detect if the current member is a template.
            var isTemplate = !this._symbolScopeClassifier.GetTemplateInfo( symbol ).IsNone
                             || (symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol }
                                 && !this._symbolScopeClassifier.GetTemplateInfo( associatedSymbol ).IsNone);

            // If it is a template, update the currentTemplateMember field.
            if ( isTemplate )
            {
                var previousTemplateMember = this._currentTemplateMember;
                this._currentTemplateMember = symbol;

                try
                {
                    // We don't want to visit the whole member because only the implementation must be annotated and transformed
                    // as a template.
                    return node.WithInitializer( this.Visit( node.Initializer ) ).AddIsTemplateAnnotation();
                }
                finally
                {
                    this._currentTemplateMember = previousTemplateMember;
                }
            }
            else
            {
                // Don't visit members that are not templates.
                return node;
            }
        }

        TemplatingScope localScope;
        var transformedInitializer = node.Initializer?.WithValue( this.Visit( node.Initializer.Value ) );

        // Decide of the scope of the local variable.
        if ( this._currentScopeContext.ForceCompileTimeOnlyExpression )
        {
            localScope = CompileTimeOnly;
        }
        else
        {
            // Infer the variable scope from the initializer.

            if ( transformedInitializer != null )
            {
                localScope = this.GetNodeScope( transformedInitializer.Value )
                    .GetExpressionValueScope( true );

                if ( localScope.IsUndetermined() )
                {
                    localScope = RunTimeOnly;
                }
            }
            else
            {
                // Variables without initializer have runtime scope.
                localScope = RunTimeOnly;
            }
        }

        // Mark the local variable symbol.
        this.SetLocalSymbolScope( local, localScope );

        var transformedIdentifier = node.Identifier;

        // Mark the identifier for syntax highlighting.
        if ( localScope == CompileTimeOnly )
        {
            transformedIdentifier = transformedIdentifier.AddColoringAnnotation( TextSpanClassification.CompileTimeVariable );
        }

        // Transform arguments.
        ArgumentSyntax[]? transformedArguments = null;

        if ( node.ArgumentList != null )
        {
            using ( this.WithScopeContext(
                       localScope == CompileTimeOnly
                           ? this._currentScopeContext.CompileTimeOnly( "creation of a compile-time object" )
                           : this._currentScopeContext.RunTimePreferred( "creation of a run-time object" ) ) )
            {
                transformedArguments = node.ArgumentList.Arguments.SelectAsArray( a => this.Visit( a ) );
            }
        }

        var transformedArgumentList = transformedArguments != null
            ? BracketedArgumentList(
                node.ArgumentList!.OpenBracketToken,
                SeparatedList( transformedArguments, node.ArgumentList.Arguments.GetSeparators() ),
                node.ArgumentList.CloseBracketToken )
            : null;

        return node.Update( transformedIdentifier, transformedArgumentList, transformedInitializer ).AddScopeAnnotation( localScope );
    }

    public override SyntaxNode VisitVariableDeclaration( VariableDeclarationSyntax node )
    {
        var transformedType = this.Visit( node.Type );

        if ( this._templateMemberClassifier.IsNodeOfDynamicType( transformedType ) )
        {
            foreach ( var variable in node.Variables )
            {
                if ( variable.Initializer == null )
                {
                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.CannotUseDynamicInUninitializedLocal,
                        variable.Identifier,
                        variable.Identifier.Text );
                }
                else
                {
                    if ( !this._templateMemberClassifier.IsNodeOfDynamicType( variable.Initializer.Value ) )
                    {
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.DynamicVariableSetToNonDynamic,
                            variable.Identifier,
                            variable.Identifier.Text );
                    }
                }
            }
        }

        if ( this.GetNodeScope( transformedType ) == CompileTimeOnly )
        {
            var typeSymbol = this._syntaxTreeAnnotationMap.GetExpressionType( node.Type )!;

            using ( this.WithScopeContext( this._currentScopeContext.CompileTimeOnly( $"a local variable of compile-time-only type '{typeSymbol}'" ) ) )
            {
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                var transformedVariables = node.Variables.SelectAsReadOnlyList( v => this.Visit( v )! );

                return node.Update( transformedType, SeparatedList( transformedVariables ) ).AddScopeAnnotation( CompileTimeOnly );
            }
        }
        else
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            var transformedVariables = node.Variables.SelectAsImmutableArray( v => this.Visit( v )! );

            var variableScopes = transformedVariables.SelectAsReadOnlyList( v => v.GetScopeFromAnnotation() ).Distinct().ToReadOnlyList();

            if ( variableScopes.Count != 1 )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.SplitVariables,
                    node,
                    string.Join( ",", node.Variables.SelectAsReadOnlyList( v => "'" + v.Identifier.Text + "'" ) ) );
            }

            var variableScope = variableScopes.Single();

            // We don't use transformedType because we want to replace the type annotation to strictly RunTime and not, for instance, CompileTimeReturningRunTime.
            return node.Update( node.Type.AddScopeAnnotation( variableScope ), SeparatedList( transformedVariables ) ).AddScopeAnnotation( variableScope );
        }
    }

    public override SyntaxNode VisitLocalDeclarationStatement( LocalDeclarationStatementSyntax node )
    {
        var transformedNode = (LocalDeclarationStatementSyntax) base.VisitLocalDeclarationStatement( node )!;

        return transformedNode.AddScopeAnnotation( this.GetNodeScope( transformedNode.Declaration ) );
    }

    #endregion

    public override SyntaxNode VisitAttribute( AttributeSyntax node )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node.Name );

        if ( symbol is IMethodSymbol { ContainingNamespace: not null } constructor
             && (constructor.ContainingNamespace.ToString()?.StartsWith( "Metalama.Framework", StringComparison.Ordinal ) ?? false) )
        {
            node = node.AddColoringAnnotation( TextSpanClassification.CompileTime );
        }

        // Otherwise, don't process attributes.
        return node;
    }

    public override SyntaxNode VisitAttributeList( AttributeListSyntax node )
    {
        var annotatedList = (AttributeListSyntax) base.VisitAttributeList( node )!;

        if ( annotatedList.Attributes.All( a => a.GetColorFromAnnotation() == TextSpanClassification.CompileTime ) )
        {
            return annotatedList.AddColoringAnnotation( TextSpanClassification.CompileTime );
        }
        else
        {
            return annotatedList;
        }
    }

    private T VisitMemberDeclaration<T>( T node, Func<T, T> visitImplementation )
        where T : SyntaxNode
    {
        var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node )!;

        // Detect if the current member is a template.
        var templateInfo = this._symbolScopeClassifier.GetTemplateInfo( symbol );

        // If it is a template, update the currentTemplateMember field.
        if ( !templateInfo.IsNone )
        {
            var previousTemplateMember = this._currentTemplateMember;
            this._currentTemplateMember = symbol;

            if ( templateInfo.AttributeType == TemplateAttributeType.Template )
            {
                var isVoid = symbol is IMethodSymbol methodSymbol &&
                             (methodSymbol.ReturnsVoid ||
                              (AsyncHelper.TryGetAsyncInfo( methodSymbol.ReturnType, out var resultType, out _ ) &&
                               resultType.SpecialType == SpecialType.System_Void));

                if ( isVoid && node is MethodDeclarationSyntax { Body: { } body } )
                {
                    // Check void template methods for redundant return statements.
                    RedundantReturnVisitor.ReportErrors( this, body );
                }
            }

            try
            {
                // We don't want to visit the whole member because only the implementation must be annotated and transformed
                // as a template.
                return visitImplementation( node ).AddIsTemplateAnnotation();
            }
            finally
            {
                this._currentTemplateMember = previousTemplateMember;
            }
        }
        else
        {
            // Don't visit members that are not templates.
            return node;
        }
    }

    // This method is called from the classification service. Constructors are never templates, so it can be skipped.
    public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node ) => node;

    public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
        => this.VisitMemberDeclaration(
            node,
            n => node
                .WithBody( this.Visit( n.Body ) )
                .WithExpressionBody( this.Visit( n.ExpressionBody ) )
                .WithAttributeLists( this.VisitList( node.AttributeLists ) )
                .WithParameterList( this.Visit( node.ParameterList ) )
                .WithReturnType( this.Visit( node.ReturnType ) )
                .WithTypeParameterList( this.Visit( node.TypeParameterList ) ) );

    public override SyntaxNode VisitParameter( ParameterSyntax node )
    {
        var annotatedNode = (ParameterSyntax) base.VisitParameter( node )!;

        if ( this._currentTemplateMember != null )
        {
            if ( this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node ) is IParameterSymbol symbol )
            {
                var scope = this.GetSymbolScope( symbol );

                if ( scope.GetExpressionExecutionScope() == CompileTimeOnly )
                {
                    annotatedNode = annotatedNode.AddColoringAnnotation( TextSpanClassification.CompileTime );

                    var parameterTypeScope = this.GetSymbolScope( symbol.Type );

                    // Compile-time parameters of run-time-only types are not allowed.
                    if ( parameterTypeScope.GetExpressionValueScope() == RunTimeOnly )
                    {
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.CompileTimeTemplateParameterWithRunTimeType,
                            node,
                            (symbol.Name, symbol.Type) );
                    }
                }

                if ( this._isInLocalFunction )
                {
                    // Make sure compile-time local function parameters produce errors.
                    var classifierScope = this._symbolScopeClassifier.GetTemplatingScope( symbol );

                    this.RequireScope( node, classifierScope.GetExpressionExecutionScope(), RunTimeOnly, "a template local function" );
                }
                else if ( symbol.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.LambdaMethod } )
                {
                    this._templateProjectManifestBuilder?.AddOrUpdateSymbol( symbol, scope );
                }
            }
        }

        return annotatedNode;
    }

    public override SyntaxNode VisitTypeParameter( TypeParameterSyntax node )
    {
        var annotatedNode = base.VisitTypeParameter( node )!;

        if ( this._currentTemplateMember != null )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

            if ( symbol != null )
            {
                var scope = this.GetSymbolScope( symbol );

                if ( scope == CompileTimeOnlyReturningRuntimeOnly )
                {
                    annotatedNode = annotatedNode.AddColoringAnnotation( TextSpanClassification.CompileTime );
                }

                if ( !this._isInLocalFunction )
                {
                    this._templateProjectManifestBuilder?.AddOrUpdateSymbol( symbol, scope );
                }
            }
        }

        return annotatedNode;
    }

    public override SyntaxNode VisitAccessorDeclaration( AccessorDeclarationSyntax node )
        => this.VisitMemberDeclaration(
            node,
            n => node.WithBody( this.Visit( n.Body ) )
                .WithExpressionBody( this.Visit( n.ExpressionBody ) )
                .WithAttributeLists( this.VisitList( node.AttributeLists ) ) );

    public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node )
        => this.VisitMemberDeclaration(
            node,
            n => n.WithAccessorList( this.Visit( n.AccessorList ) )
                .WithExpressionBody( this.Visit( n.ExpressionBody ) )
                .WithInitializer( this.Visit( n.Initializer ) )
                .WithAttributeLists( this.VisitList( node.AttributeLists ) ) );

    public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node )
        => this.VisitMemberDeclaration( node, n => n.WithAccessorList( this.Visit( n.AccessorList ) ) );

    private static bool IsMutatingUnaryOperator( SyntaxToken token ) => token.Kind() is SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken;

    public override SyntaxNode VisitPostfixUnaryExpression( PostfixUnaryExpressionSyntax node )
    {
        var (transformedOperand, scope) = this.VisitUnaryExpressionOperand( node.Operand, node.OperatorToken );

        return node.Update( transformedOperand, node.OperatorToken ).WithSymbolAnnotationsFrom( node ).AddScopeAnnotation( scope );
    }

    public override SyntaxNode VisitPrefixUnaryExpression( PrefixUnaryExpressionSyntax node )
    {
        var (transformedOperand, scope) = this.VisitUnaryExpressionOperand( node.Operand, node.OperatorToken );

        return node.Update( node.OperatorToken, transformedOperand ).WithSymbolAnnotationsFrom( node ).AddScopeAnnotation( scope );
    }

    private (ExpressionSyntax TransformedOperand, TemplatingScope Scope) VisitUnaryExpressionOperand( ExpressionSyntax operand, SyntaxToken @operator )
    {
        var transformedOperand = this.Visit( operand );

        var scope = this.GetNodeScope( transformedOperand );

        if ( @operator.IsKind( SyntaxKind.ExclamationToken ) && scope.GetExpressionValueScope() == RunTimeOnly )
        {
            scope = RunTimeOnly;
        }

        if ( IsMutatingUnaryOperator( @operator ) )
        {
            scope = this.GetAssignmentScope( transformedOperand );

            if ( scope.GetExpressionExecutionScope() == CompileTimeOnly )
            {
                this.CheckForMutatingCompileTimeExpressionInRunTimeConditionalBlock( operand );
            }
        }

        return (transformedOperand, scope);
    }

    public override SyntaxNode VisitAssignmentExpression( AssignmentExpressionSyntax node )
    {
        if ( node.Parent is InitializerExpressionSyntax )
        {
            // In an initializer assignment, the scope is determined by the right side but the transformation always proceed according to the parent.
            var transformedRight = this.Visit( node.Right );
            var scope = this.GetNodeScope( transformedRight );

            return node.Update( node.Left, node.OperatorToken, transformedRight )
                .AddScopeAnnotation( scope )
                .AddTargetScopeAnnotation( MustFollowParent );
        }
        else
        {
            // The scope of a classical assignment is determined by the left side.
            var transformedLeft = this.Visit( node.Left );

            var leftScope = this.GetAssignmentScope( transformedLeft );
            ExpressionSyntax? transformedRight;

            // If we are in a run-time-conditional block, we cannot assign compile-time variables.
            ScopeContext? context = null;

            if ( leftScope.GetExpressionExecutionScope() == CompileTimeOnly )
            {
                this.CheckForMutatingCompileTimeExpressionInRunTimeConditionalBlock( node.Left );

                if ( this._syntaxTreeAnnotationMap.GetExpressionType( node.Left ) is INamedTypeSymbol { Name: nameof(IExpression) } )
                {
                    // Assigning a run-time expression to an IExpression is allowed but requires special processing.
                    // It is similar to the case with a cast to IExpression, but the cast is not required.
                }
                else
                {
                    // The right part must be compile-time.
                    context = this._currentScopeContext.CompileTimeOnly( "the assignment of a compile-time expression" );
                }
            }

            using ( this.WithScopeContext( context ) )
            {
                transformedRight = this.Visit( node.Right );
            }

            // If we have a discard assignment, take the scope from the right.
            if ( leftScope == RunTimeOrCompileTime
                 && this._syntaxTreeAnnotationMap.GetSymbol( node.Left ) is IDiscardSymbol )
            {
                leftScope = this.GetNodeScope( transformedRight ).GetExpressionExecutionScope();
            }

            return node.Update( transformedLeft, node.OperatorToken, transformedRight ).AddScopeAnnotation( leftScope );
        }
    }

    private void CheckForMutatingCompileTimeExpressionInRunTimeConditionalBlock( ExpressionSyntax expression )
    {
        if ( this._currentScopeContext.IsRunTimeConditionalBlock )
        {
            if ( this._syntaxTreeAnnotationMap.GetSymbol( expression ) is not ILocalSymbol local )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.CannotSetCompileTimeExpressionInRunTimeConditionalBlock,
                    expression,
                    (expression.ToString(), this._currentScopeContext.IsRunTimeConditionalBlockReason) );
            }
            else if ( !this._currentScopeContext.RunTimeConditionalBlockVariables.Contains( local ) )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.CannotSetCompileTimeVariableInRunTimeConditionalBlock,
                    expression,
                    (expression.ToString(), this._currentScopeContext.IsRunTimeConditionalBlockReason) );
            }
        }
    }

    public override SyntaxNode VisitExpressionStatement( ExpressionStatementSyntax node )
    {
        var transformedExpression = this.Visit( node.Expression );
        var expressionScope = this.GetNodeScope( transformedExpression );
        var statementScope = expressionScope.GetExpressionExecutionScope().ReplaceIndeterminate( RunTimeOnly );

        return node.WithExpression( transformedExpression ).AddScopeAnnotation( expressionScope ).AddTargetScopeAnnotation( statementScope );
    }

    public override SyntaxNode VisitCastExpression( CastExpressionSyntax node )
    {
        if ( this._templateMemberClassifier.IsNodeOfDynamicType( node.Type ) )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                node.Type.GetLocation(),
                default );
        }

        TypeSyntax annotatedType;

        // The scope of the type shouldn't be influenced by parent scope.
        using ( this.WithScopeContext( this._currentScopeContext.RunTimeOrCompileTime( "type to cast to" ) ) )
        {
            annotatedType = this.Visit( node.Type );
        }

        var typeScope = this.GetNodeScope( annotatedType );

        ExpressionSyntax annotatedExpression;
        TemplatingScope castScope;
        var targetRequiresUserExpression = false;

        if ( typeScope == RunTimeOnly )
        {
            // The whole cast is run-time only.
            using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( $"cast to the run-time-only type '{node.Type}'" ) ) )
            {
                annotatedExpression = this.Visit( node.Expression );
            }

            var expressionScope = this.GetNodeScope( annotatedExpression );
            castScope = this.GetExpressionScope( [annotatedExpression, annotatedType], [expressionScope, typeScope], node );
        }
        else
        {
            var type = this._syntaxTreeAnnotationMap.GetSymbol( node.Type );
            var typeIsIExpression = type is INamedTypeSymbol { Name: nameof(IExpression) };

            if ( typeIsIExpression && this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression ) is IDynamicTypeSymbol )
            {
                // This is the special case of casting dynamic to IExpression.
                using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( $"cast to IExpression" ) ) )
                {
                    annotatedExpression = this.Visit( node.Expression );
                }

                castScope = CompileTimeOnly;
                targetRequiresUserExpression = true;
            }
            else
            {
                annotatedExpression = this.Visit( node.Expression );

                var expressionScope = this.GetNodeScope( annotatedExpression );

                if ( typeScope == CompileTimeOnly && expressionScope.GetExpressionValueScope() == RunTimeOnly )
                {
                    if ( typeIsIExpression )
                    {
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.CannotCastRunTimeExpressionToIExpression,
                            node.Type,
                            node.Expression.ToString() );
                    }
                    else
                    {
                        // We cannot cast a run-time expression to a compile-time type, except to IExpression.
                        this.ReportDiagnostic(
                            TemplatingDiagnosticDescriptors.CannotCastRunTimeExpressionToCompileTimeType,
                            node.Type,
                            (node.Expression.ToString(), node.Type.ToString()) );
                    }

                    // Act as if the cast worked, to suppress other errors.
                    castScope = CompileTimeOnly;
                }
                else
                {
                    castScope = this.GetExpressionScope( [annotatedExpression, annotatedType], [expressionScope, typeScope], node );
                }
            }
        }

        return node.Update( node.OpenParenToken, annotatedType, node.CloseParenToken, annotatedExpression )
            .AddScopeAnnotation( castScope )
            .AddTargetRequiresUserExpressionAnnotation( targetRequiresUserExpression );
    }

    public override SyntaxNode VisitBinaryExpression( BinaryExpressionSyntax node )
    {
        switch ( node.Kind() )
        {
            case SyntaxKind.IsExpression:
            case SyntaxKind.AsExpression:
                if ( this._templateMemberClassifier.IsNodeOfDynamicType( node.Right ) )
                {
                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                        node.Right.GetLocation(),
                        default );
                }

                var annotatedType = (TypeSyntax) this.Visit( node.Right );
                var annotatedExpression = this.Visit( node.Left );
                var transformedNode = node.WithLeft( annotatedExpression ).WithRight( annotatedType );

                return this.AnnotateCastExpression( transformedNode, annotatedType, annotatedExpression );

            case SyntaxKind.CoalesceExpression:
                return this.VisitCoalesceExpression( node );
        }

        var visitedNode = base.VisitBinaryExpression( node )!;

        return this.AddScopeAnnotationToVisitedNode( node, visitedNode );
    }

    private SyntaxNode AnnotateCastExpression( ExpressionSyntax transformedCastNode, TypeSyntax annotatedType, ExpressionSyntax annotatedExpression )
    {
        var combinedScope = this.GetNodeScope( annotatedType ) == RunTimeOrCompileTime
            ? this.GetNodeScope( annotatedExpression ).GetExpressionValueScope()
            : this.GetExpressionScope( [annotatedExpression], transformedCastNode );

        return transformedCastNode.AddScopeAnnotation( combinedScope );
    }

    private SyntaxNode VisitCoalesceExpression( BinaryExpressionSyntax node )
    {
        // The scope is determined by the left part, unless the left part is indeterminate. The right part must follow.

        var annotatedLeft = this.Visit( node.Left );
        var leftScope = this.GetNodeScope( annotatedLeft );

        ExpressionSyntax annotatedRight;
        TemplatingScope combinedScope;

        if ( !leftScope.IsUndetermined() )
        {
            ScopeContext context;

            if ( leftScope.EvaluatesToRunTimeValue() )
            {
                context = this._currentScopeContext.RunTimePreferred( $"right part of the run-time '{node.Left} ??'" );
                combinedScope = RunTimeOnly;
            }
            else
            {
                context = this._currentScopeContext.CompileTimeOnly( $"right part of the compile-time '{node.Left} ??'" );
                combinedScope = leftScope;
            }

            using ( this.WithScopeContext( context ) )
            {
                annotatedRight = this.Visit( node.Right );
            }
        }
        else
        {
            // Use the default rule.
            annotatedRight = this.Visit( node.Right );
            var rightScope = this.GetNodeScope( annotatedRight );
            combinedScope = this.GetExpressionScope( [annotatedLeft, annotatedRight], [leftScope, rightScope], node );
        }

        return node.Update( annotatedLeft, node.OperatorToken, annotatedRight ).AddScopeAnnotation( combinedScope );
    }

    public override SyntaxNode VisitConditionalExpression( ConditionalExpressionSyntax node )
    {
        var annotatedCondition = this.Visit( node.Condition );
        var conditionScope = this.GetNodeScope( annotatedCondition );

        ExpressionSyntax annotatedWhenTrue;
        ExpressionSyntax annotatedWhenFalse;
        ScopeContext? scopeContext;

        if ( conditionScope.GetExpressionExecutionScope( true ) == CompileTimeOnly )
        {
            scopeContext = null;
        }
        else
        {
            scopeContext = this._currentScopeContext.RunTimePreferred( $"run-time conditional expression with the condition '{node.Condition}'" );
        }

        using ( this.WithScopeContext( scopeContext ) )
        {
            annotatedWhenTrue = this.Visit( node.WhenTrue );
            annotatedWhenFalse = this.Visit( node.WhenFalse );
        }

        var combinedScope = RunTimeOnly;

        // Mark the whole expression as compile-time only if all three sub-expressions are compile-time
        if ( scopeContext == null &&
             this.GetNodeScope( annotatedWhenTrue ).GetExpressionExecutionScope( true ) == CompileTimeOnly &&
             this.GetNodeScope( annotatedWhenFalse ).GetExpressionExecutionScope( true ) == CompileTimeOnly )
        {
            combinedScope = CompileTimeOnly;
        }

        return node.Update(
                annotatedCondition,
                node.QuestionToken,
                annotatedWhenTrue,
                node.ColonToken,
                annotatedWhenFalse )
            .AddScopeAnnotation( combinedScope );
    }

    public override SyntaxNode VisitForStatement( ForStatementSyntax node )
    {
        // This is a quick-and-dirty implementation that all for statements runtime.

        var transformedVariableDeclaration = this.Visit( node.Declaration );

        if ( transformedVariableDeclaration != null )
        {
            this.RequireScope( transformedVariableDeclaration.Variables, RunTimeOnly, "variable of a 'for' loop" );
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        var transformedInitializers = node.Initializers.SelectAsReadOnlyList( i => this.Visit( i )! );
        var transformedCondition = this.Visit( node.Condition );

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        var transformedIncrementors = node.Incrementors.SelectAsReadOnlyList( syntax => this.Visit( syntax )! );

        StatementSyntax transformedStatement;

        using ( this.WithScopeContext(
                   this._currentScopeContext.BreakOrContinue(
                       RunTimeOnly,
                       $"for ( {node.Initializers}; ... )" ) ) )
        {
            transformedStatement = this.Visit( node.Statement );
        }

        return ForStatement(
                node.ForKeyword,
                node.OpenParenToken,
                transformedVariableDeclaration,
                SeparatedList( transformedInitializers ),
                node.FirstSemicolonToken,
                transformedCondition,
                node.SecondSemicolonToken,
                SeparatedList( transformedIncrementors ),
                node.CloseParenToken,
                transformedStatement )
            .AddTargetScopeAnnotation( RunTimeOnly );
    }

    public override SyntaxNode VisitWhileStatement( WhileStatementSyntax node )
    {
        // The scope of a `while` statement is determined by its condition only.

        var annotatedCondition = this.Visit( node.Condition ).ReplaceScopeAnnotationIfUndetermined( RunTimeOnly );
        var conditionScope = this.GetNodeScope( annotatedCondition ).GetExpressionExecutionScope( preferCompileTime: true );

        this.RequireLoopScope( node.Condition, conditionScope, "while" );

        StatementSyntax annotatedStatement;

        using ( this.WithScopeContext( this._currentScopeContext.BreakOrContinue( conditionScope, $"while ( {node.Condition} )" ) ) )
        {
            annotatedStatement = this.Visit( node.Statement ).ReplaceScopeAnnotation( conditionScope );
        }

        return node.Update(
                node.AttributeLists,
                node.WhileKeyword,
                node.OpenParenToken,
                annotatedCondition,
                node.CloseParenToken,
                annotatedStatement.AddTargetScopeAnnotation( conditionScope ) )
            .AddScopeAnnotation( conditionScope );
    }

    public override SyntaxNode VisitDoStatement( DoStatementSyntax node )
    {
        // The scope of a `do ... while` statement is determined by its condition only.

        var annotatedCondition = this.Visit( node.Condition ).ReplaceScopeAnnotationIfUndetermined( RunTimeOnly );
        var conditionScope = this.GetNodeScope( annotatedCondition ).GetExpressionExecutionScope( preferCompileTime: true );

        this.RequireLoopScope( node.Condition, conditionScope, "do" );

        StatementSyntax annotatedStatement;

        using ( this.WithScopeContext( this._currentScopeContext.BreakOrContinue( conditionScope, $"do ... while ( {node.Condition} )" ) ) )
        {
            annotatedStatement = this.Visit( node.Statement ).ReplaceScopeAnnotation( conditionScope );
        }

        return node.Update(
                node.AttributeLists,
                node.DoKeyword,
                annotatedStatement.AddTargetScopeAnnotation( conditionScope ),
                node.WhileKeyword,
                node.OpenParenToken,
                annotatedCondition,
                node.CloseParenToken,
                node.SemicolonToken )
            .AddScopeAnnotation( conditionScope );
    }

    public override SyntaxNode VisitReturnStatement( ReturnStatementSyntax node )
    {
        if ( this._currentScopeContext.ForceCompileTimeOnlyExpression )
        {
            // We are in a compile-time lambda.
            return base.VisitReturnStatement( node )!.AddScopeAnnotation( CompileTimeOnly );
        }
        else
        {
            return base.VisitReturnStatement( node )!.AddScopeAnnotation( RunTimeOnly );
        }
    }

    #region Unsupported Features

    private void ReportUnsupportedLanguageFeature( SyntaxNodeOrToken nodeForDiagnostic, string featureName )
        => this.ReportDiagnostic( TemplatingDiagnosticDescriptors.LanguageFeatureIsNotSupported, nodeForDiagnostic, featureName );

    public override SyntaxNode? VisitUnsafeStatement( UnsafeStatementSyntax node )
    {
        this.ReportUnsupportedLanguageFeature( node.UnsafeKeyword, "unsafe" );

        return base.VisitUnsafeStatement( node );
    }

    public override SyntaxNode? VisitGotoStatement( GotoStatementSyntax node )
    {
        this.ReportUnsupportedLanguageFeature( node.GotoKeyword, "goto" );

        return base.VisitGotoStatement( node );
    }

    public override SyntaxNode VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
    {
        foreach ( var attributeList in node.AttributeLists )
        {
            foreach ( var attribute in attributeList.Attributes )
            {
                var attributeTypeSymbol = (this._syntaxTreeAnnotationMap.GetSymbol( attribute.Name ) as IMethodSymbol)?.ContainingType;

                if ( attributeTypeSymbol != null
                     && (attributeTypeSymbol.AllInterfaces.Any( i => i.GetFullName() == "Metalama.Framework.Advising.ITemplateAttribute" )
                         || attributeTypeSymbol.AnyBaseType( b => b.GetFullName() == "Metalama.Framework.Aspects.ScopeAttribute" )) )
                {
                    this.ReportDiagnostic( TemplatingDiagnosticDescriptors.TemplateAttributeOnLocalFunction, attribute, attributeTypeSymbol );
                }
            }
        }

        var reason = $"local function '{node.Identifier.Text}'";

        var oldIsInLocalFunction = this._isInLocalFunction;
        this._isInLocalFunction = true;

        try
        {
            using ( this.WithScopeContext( this._currentScopeContext.ForbidDynamic( reason ) ) )
            {
                this.Visit( node.ReturnType );
                this.Visit( node.ParameterList );
            }

            using ( this.WithScopeContext( this._currentScopeContext.RunTimeConditional( reason ) ) )
            {
                if ( node.ExpressionBody != null )
                {
                    var transformedExpression = this.Visit( node.ExpressionBody.Expression );

                    return node.WithExpressionBody( node.ExpressionBody.WithExpression( transformedExpression ) )
                        .AddScopeAnnotation( RunTimeOnly );
                }
                else
                {
                    var transformedBody = this.Visit( node.Body );

                    return node.WithBody( transformedBody ).AddScopeAnnotation( RunTimeOnly );
                }
            }
        }
        finally
        {
            this._isInLocalFunction = oldIsInLocalFunction;
        }
    }

    public override SyntaxNode VisitAnonymousMethodExpression( AnonymousMethodExpressionSyntax node )
        => this.VisitAnonymousFunctionExpression(
            node,
            node.DelegateKeyword,
            node.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>(),
            base.VisitAnonymousMethodExpression );

    public override SyntaxNode VisitQueryExpression( QueryExpressionSyntax node )
    {
        this.ReportUnsupportedLanguageFeature( node.FromClause.FromKeyword, "LINQ" );

        return node;
    }

    public override SyntaxNode VisitAwaitExpression( AwaitExpressionSyntax node )
    {
        // Await is always run-time.

        ExpressionSyntax transformedExpression;

        using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( "'await' expression" ) ) )
        {
            transformedExpression = this.Visit( node.Expression );
        }

        return node.WithExpression( transformedExpression ).AddScopeAnnotation( RunTimeOnly );
    }

    public override SyntaxNode VisitYieldStatement( YieldStatementSyntax node )
    {
        // Yield is always run-time.

        ExpressionSyntax? transformedExpression;

        if ( node.Expression != null )
        {
            using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( "'yield' expression" ) ) )
            {
                transformedExpression = this.Visit( node.Expression );
            }
        }
        else
        {
            transformedExpression = null;
        }

        return node.WithExpression( transformedExpression ).AddScopeAnnotation( RunTimeOnly );
    }

    #endregion

    #region Lambda expressions

    private SyntaxNode VisitAnonymousFunctionExpression<T>(
        T node,
        SyntaxToken tokenForDiagnostic,
        IEnumerable<ParameterSyntax> parameters,
        Func<T, SyntaxNode?> callBase )
        where T : AnonymousFunctionExpressionSyntax
    {
        var wellKnownScope = this._currentScopeContext.PreferRunTimeExpression ? RunTimeOnly
            : this._currentScopeContext.ForceCompileTimeOnlyExpression ? CompileTimeOnly
            : LateBound;

        // Remember the scope of the parameters.
        if ( wellKnownScope != LateBound )
        {
            foreach ( var parameter in parameters )
            {
                var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( parameter );

                if ( symbol != null )
                {
                    this.SetLocalSymbolScope( symbol, wellKnownScope );
                }
            }
        }

        if ( node.ExpressionBody != null )
        {
            // Lambda expressions returning a dynamic type are not supported because they change the return
            // type of the whole invocation expression when using LINQ selects. Allowing this would require more handling
            // and testing.
            if ( this._syntaxTreeAnnotationMap.GetExpressionType( node.ExpressionBody ) is IDynamicTypeSymbol )
            {
                this.ReportDiagnostic( TemplatingDiagnosticDescriptors.DynamicInLambdaUnsupported, node, default );
            }

            var annotatedExpression = this.Visit( node.ExpressionBody );

            return node.WithExpressionBody( annotatedExpression ).WithScopeAnnotationFrom( annotatedExpression );
        }
        else if ( wellKnownScope != LateBound )
        {
            var scope = this._currentScopeContext.PreferRunTimeExpression ? RunTimeOnly : CompileTimeOnly;

            var annotateBlock = (BlockSyntax) this.VisitBlock( node.Block! )!;

            return node.WithBlock( annotateBlock ).AddTargetScopeAnnotation( scope );
        }
        else
        {
            // We must know the scope.
            this.ReportDiagnostic( TemplatingDiagnosticDescriptors.UnknownScopedAnonymousMethod, tokenForDiagnostic, default );

            return callBase( node )!;
        }
    }

    public override SyntaxNode VisitParenthesizedLambdaExpression( ParenthesizedLambdaExpressionSyntax node )
    {
        if ( node.ReturnType != null && this._templateMemberClassifier.IsNodeOfDynamicType( node.ReturnType ) )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                node.ReturnType.GetLocation(),
                default );
        }

        foreach ( var parameter in node.ParameterList.Parameters )
        {
            if ( parameter.Type != null && this._templateMemberClassifier.IsNodeOfDynamicType( parameter.Type ) )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                    parameter.Type.GetLocation(),
                    default );
            }
        }

        return this.VisitAnonymousFunctionExpression( node, node.ArrowToken, node.ParameterList.Parameters, base.VisitParenthesizedLambdaExpression );
    }

    public override SyntaxNode VisitSimpleLambdaExpression( SimpleLambdaExpressionSyntax node )
        => this.VisitAnonymousFunctionExpression( node, node.ArrowToken, [node.Parameter], base.VisitSimpleLambdaExpression );

    #endregion

    #region Switch

    public override SyntaxNode VisitSwitchExpressionArm( SwitchExpressionArmSyntax node )
    {
        var transformedPattern = this.Visit( node.Pattern );
        var patternScope = this.GetNodeScope( transformedPattern );

        var transformedWhen = this.Visit( node.WhenClause );
        var transformedExpression = this.Visit( node.Expression );

        TemplatingScope combinedScope;

        if ( patternScope == CompileTimeOnly )
        {
            // If the pattern is build-time only, then the whole arm is build-time only.
            combinedScope = CompileTimeOnly;
        }
        else
        {
            combinedScope = this.GetSwitchCaseScope( transformedPattern, transformedWhen, transformedExpression, node );
        }

        return node.Update(
                transformedPattern,
                transformedWhen,
                node.EqualsGreaterThanToken,
                transformedExpression )
            .AddScopeAnnotation( combinedScope )
            .AddTargetScopeAnnotation( MustFollowParent );
    }

    public override SyntaxNode VisitSwitchExpression( SwitchExpressionSyntax node )
    {
        var transformedGoverningExpression = this.Visit( node.GoverningExpression );
        var governingExpressionScope = transformedGoverningExpression.GetScopeFromAnnotation().GetValueOrDefault();

        if ( (governingExpressionScope == CompileTimeOnly
              && this._templateMemberClassifier.IsNodeOfDynamicType( transformedGoverningExpression ))
             || governingExpressionScope != CompileTimeOnly )
        {
            governingExpressionScope = RunTimeOnly;
        }

        var armContext = governingExpressionScope == CompileTimeOnly
            ? this._currentScopeContext.CompileTimeOnly( "a compile-time switch expression" )
            : null;

        SwitchExpressionArmSyntax[] transformedArms;

        using ( this.WithScopeContext( armContext ) )
        {
            transformedArms = node.Arms.SelectAsArray( a => this.Visit( a ) );

            var reason = governingExpressionScope == RunTimeOnly
                ? "a run-time switch expression"
                : "a compile-time switch expression";

            this.RequireScope( transformedArms, governingExpressionScope, reason );
        }

        return node.Update(
                transformedGoverningExpression,
                node.SwitchKeyword,
                node.OpenBraceToken,
                SeparatedList( transformedArms, node.Arms.GetSeparators() ),
                node.CloseBraceToken )
            .AddScopeAnnotation( governingExpressionScope );
    }

    public override SyntaxNode VisitSwitchStatement( SwitchStatementSyntax node )
    {
        var annotatedExpression = this.Visit( node.Expression );
        var expressionScope = annotatedExpression.GetScopeFromAnnotation() ?? RunTimeOrCompileTime;

        TemplatingScope switchScope;
        string scopeReason;

        if ( (expressionScope == CompileTimeOnly && this._templateMemberClassifier.IsNodeOfDynamicType( annotatedExpression ))
             || expressionScope.GetExpressionValueScope( true ).ReplaceIndeterminate( CompileTimeOnly )
             != CompileTimeOnly )
        {
            switchScope = RunTimeOnly;
            scopeReason = $"the run-time 'switch( {node.Expression} )'";
        }
        else
        {
            switchScope = CompileTimeOnly;
            scopeReason = $"the compile-time 'switch( {node.Expression} )'";
        }

        var transformedSections = new SwitchSectionSyntax[node.Sections.Count];

        for ( var i = 0; i < node.Sections.Count; i++ )
        {
            var section = node.Sections[i];

            SwitchLabelSyntax[] transformedLabels;
            StatementSyntax[] transformedStatements;

            var labelContext = switchScope == CompileTimeOnly
                ? this._currentScopeContext.CompileTimeOnly( scopeReason )
                : this._currentScopeContext.RunTimePreferred( scopeReason );

            using ( this.WithScopeContext( labelContext ) )
            {
                transformedLabels = section.Labels.SelectAsArray( this.Visit )!;

                if ( this.RequireScope( transformedLabels, switchScope, scopeReason ) )
                {
                    transformedLabels = transformedLabels.SelectAsArray( l => l.ReplaceScopeAnnotation( switchScope ) );
                }
                else
                {
                    // We would have an error if we replace the annotation.
                }
            }

            using ( this.WithScopeContext( this._currentScopeContext.BreakOrContinue( switchScope, scopeReason ) ) )
            {
                // Statements of a compile-time control block must have an explicitly-set scope otherwise the template compiler
                // will look at the scope in the parent node, which is here incorrect.
                transformedStatements =
                    section.Statements.SelectAsArray( s => this.Visit( s ).ReplaceScopeAnnotationIfUndetermined( CompileTimeOnly ) );
            }

            transformedSections[i] = section.Update( List( transformedLabels ), List( transformedStatements ) ).AddScopeAnnotation( switchScope );
        }

        return node.Update(
                node.SwitchKeyword,
                node.OpenParenToken,
                annotatedExpression.ReplaceScopeAnnotation( switchScope ),
                node.CloseParenToken,
                node.OpenBraceToken,
                List( transformedSections ),
                node.CloseBraceToken )
            .AddScopeAnnotation( switchScope );
    }

    private TemplatingScope GetSwitchCaseScope(
        SyntaxNode transformedPattern,
        SyntaxNode? transformedWhen,
        SyntaxNode? transformedExpression,
        SyntaxNode originalNode )
        => this.GetExpressionScope( [transformedPattern, transformedWhen, transformedExpression], originalNode );

    public override SyntaxNode VisitCasePatternSwitchLabel( CasePatternSwitchLabelSyntax node )
    {
        var transformedPattern = this.Visit( node.Pattern );
        var patternScope = this.GetNodeScope( transformedPattern );
        var transformedWhen = this.Visit( node.WhenClause );

        var combinedScope = patternScope == CompileTimeOnly
            ? CompileTimeOnly
            : this.GetSwitchCaseScope( transformedPattern, transformedWhen, null, node );

        return node.Update( node.Keyword, transformedPattern, transformedWhen, node.ColonToken ).AddScopeAnnotation( combinedScope );
    }

    #endregion

    private bool RequireScope( IEnumerable<SyntaxNode> nodes, TemplatingScope requiredScope, string reason )
    {
        foreach ( var node in nodes )
        {
            if ( !this.RequireScope( node, requiredScope, reason ) )
            {
                return false;
            }
        }

        return true;
    }

    private bool RequireScope( SyntaxNode? node, TemplatingScope requiredScope, string reason )
        => this.RequireScope( node, this.GetNodeScope( node ), requiredScope, reason );

    private bool RequireScope( SyntaxNode? node, TemplatingScope existingScope, TemplatingScope requiredScope, string reason )
    {
        if ( node == null )
        {
            return true;
        }

        existingScope = existingScope.GetExpressionValueScope();
        requiredScope = requiredScope.GetExpressionExecutionScope();

        if ( existingScope != RunTimeOrCompileTime && existingScope != requiredScope )
        {
            // Don't emit an error if any descendant node already has an error because this creates redundant messages.
            if ( !node.DescendantNodes().Any( n => n.HasScopeMismatchAnnotation() ) )
            {
                this.ReportDiagnostic(
                    TemplatingDiagnosticDescriptors.ScopeMismatch,
                    node,
                    (node.ToString(), existingScope.ToDisplayString(), requiredScope.ToDisplayString(), reason) );

                return false;
            }
        }

        return true;
    }

    private void RequireLoopScope( SyntaxNode nodeForDiagnostic, TemplatingScope requiredScope, string statementName )
    {
        if ( requiredScope.GetExpressionExecutionScope() == CompileTimeOnly && this._currentScopeContext.IsRunTimeConditionalBlock )
        {
            // It is not allowed to have a do or while loop in a run-time-conditional block because compile-time loops require a compile-time
            // variable, and mutating a compile-time variable is not allowed in a run-time-conditional block. This condition may be
            // removed in the future because the loop variable may actually not be observable from outside the block, this
            // is not implemented. Since the iteration variable of a foreach loop cannot be mutated explicitly, this check does not apply there.

            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.CannotHaveCompileTimeLoopInRunTimeConditionalBlock,
                nodeForDiagnostic,
                (statementName, this._currentScopeContext.IsRunTimeConditionalBlockReason) );
        }
    }

    public override SyntaxNode VisitLockStatement( LockStatementSyntax node )
    {
        var annotatedExpression = this.Visit( node.Expression );
        var annotatedStatement = this.Visit( node.Statement );

        this.RequireScope( annotatedExpression, RunTimeOnly, "a 'lock' statement" );

        return node.Update(
                node.LockKeyword,
                node.OpenParenToken,
                annotatedExpression,
                node.CloseParenToken,
                annotatedStatement )
            .AddScopeAnnotation( RunTimeOnly );
    }

    public override SyntaxNode VisitUsingStatement( UsingStatementSyntax node )
    {
        var annotatedExpression = this.Visit( node.Expression );
        var annotatedDeclaration = this.Visit( node.Declaration );
        var annotatedStatement = this.Visit( node.Statement );

        this.RequireScope( annotatedExpression, RunTimeOnly, "a 'using' statement" );
        this.RequireScope( annotatedDeclaration, RunTimeOnly, "a 'using' statement" );

        return node.Update(
                node.AwaitKeyword,
                node.UsingKeyword,
                node.OpenParenToken,
                annotatedDeclaration,
                annotatedExpression,
                node.CloseParenToken,
                annotatedStatement )
            .AddScopeAnnotation( RunTimeOnly );
    }

    public override SyntaxNode VisitArrayType( ArrayTypeSyntax node )
    {
        var transformedNode = (ArrayTypeSyntax) base.VisitArrayType( node )!;

        var scope = this.GetNodeScope( transformedNode.ElementType );

        if ( scope == Dynamic )
        {
            // We cannot have an array of dynamic.
            this.ReportDiagnostic( TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction, node, node.ToString() );
        }

        return transformedNode;
    }

    public override SyntaxNode VisitRefType( RefTypeSyntax node )
    {
        var transformedNode = (RefTypeSyntax) base.VisitRefType( node )!;

        var scope = this.GetNodeScope( transformedNode.Type );

        if ( scope == Dynamic )
        {
            // We cannot have a ref to dynamic.
            this.ReportDiagnostic( TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction, node, node.ToString() );
        }

        return transformedNode;
    }

    private (T TransformedNode, TemplatingScope TemplatingScope, bool IsSubtemplate) VisitGenericNameCore<T>( T node, Func<T, SyntaxNode> callBase )
        where T : ExpressionSyntax
    {
        var scope = this.GetNodeScope( node );
        T transformedNode;

        var isSubtemplate = false;

        switch ( scope )
        {
            case Conflict:
                this.ReportScopeError( node );

                // We continue with an unknown scope because other methods don't handle the Conflict scope.
                scope = RunTimeOrCompileTime;
                transformedNode = node;

                break;

            case DynamicTypeConstruction:
                // We cannot have generic type instances of dynamic.
                this.ReportDiagnostic( TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction, node, node.ToString() );

                return (node, DynamicTypeConstruction, false);

            default:
                ScopeContext? context;

                if ( scope.GetExpressionExecutionScope() == CompileTimeOnly )
                {
                    var symbol = this._syntaxTreeAnnotationMap.GetInvocableSymbol( node );

                    var templateInfo = symbol == null ? TemplateInfo.None : this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol );

                    if ( templateInfo.CanBeReferencedAsSubtemplate )
                    {
                        context = this._currentScopeContext.RunTimePreferred( "a generic argument of a called template" );
                        isSubtemplate = true;
                    }
                    else
                    {
                        context = this._currentScopeContext.CompileTimeOnly( "a generic argument of compile-time declaration" );
                    }
                }
                else if ( scope.GetExpressionExecutionScope() == RunTimeOnly )
                {
                    context = this._currentScopeContext.RunTimePreferred( "a generic argument of run-time declaration" );
                }
                else
                {
                    context = null;
                }

                using ( this.WithScopeContext( context ) )
                {
                    transformedNode = (T) callBase( node );
                }

                break;
        }

        return (transformedNode.AddScopeAnnotation( scope ), scope, isSubtemplate);
    }

    public override SyntaxNode VisitGenericName( GenericNameSyntax node )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

        // If any type parameters on a member are explicitly 'dynamic', report error.
        // (Types have their own error, LAMA0227.)
        if ( symbol is not ITypeSymbol )
        {
            foreach ( var argument in node.TypeArgumentList.Arguments )
            {
                if ( this._templateMemberClassifier.IsNodeOfDynamicType( argument ) )
                {
                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                        argument,
                        default );
                }
            }
        }

        var (transformedNode, scope, isSubtemplate) = this.VisitGenericNameCore( node, base.VisitGenericName! );

        if ( isSubtemplate )
        {
            foreach ( var typeArgument in transformedNode.TypeArgumentList.Arguments )
            {
                // Subtemplate type argument has to be a run-time-only type (including compile-time template type parameter) ...
                this.RequireScope( typeArgument, RunTimeOnly, "a generic argument of a called template" );

                var typeArgumentSymbol = this._syntaxTreeAnnotationMap.GetSymbol( typeArgument );

                // ... but not a run-time template type parameter.
                if ( typeArgumentSymbol != null && this._typeParameterDetectionVisitor.Visit( typeArgumentSymbol ) )
                {
                    this.ReportDiagnostic(
                        TemplatingDiagnosticDescriptors.SubtemplateCantBeCalledWithRunTimeTypeParameter,
                        typeArgument,
                        (symbol!, typeArgument) );
                }
            }
        }

        var annotatedIdentifier = this.AddColoringAnnotations( node.Identifier, symbol, scope ).AsToken();

        return transformedNode.WithIdentifier( annotatedIdentifier );
    }

    public override SyntaxNode VisitTupleType( TupleTypeSyntax node ) => this.VisitGenericNameCore( node, base.VisitTupleType! ).TransformedNode;

    public override SyntaxNode VisitNullableType( NullableTypeSyntax node )
    {
        var transformedElementType = this.Visit( node.ElementType );
        var transformedNode = node.WithElementType( transformedElementType );

        var elementScope = transformedElementType.GetScopeFromAnnotation();

        if ( elementScope != null )
        {
            transformedNode = transformedNode.AddScopeAnnotation( elementScope.Value.GetExpressionValueScope() );
        }

        return transformedNode;
    }

    public override SyntaxNode VisitObjectCreationExpression( ObjectCreationExpressionSyntax node )
    {
        var transformedType = this.Visit( node.Type );

        var objectType = this._syntaxTreeAnnotationMap.GetExpressionType( node )
                         ?? throw new AssertionFailedException( $"Cannot get the expression type for '{node}'." );

        var objectTypeScope = this.GetSymbolScope( objectType );

        ScopeContext? context = null;

        if ( objectTypeScope == CompileTimeOnly )
        {
            context = this._currentScopeContext.CompileTimeOnly( $"the creation of an instance of the compile-time {objectType}" );
        }

        using ( this.WithScopeContext( context ) )
        {
            var transformedArguments = node.ArgumentList?.Arguments.SelectAsArray( a => this.Visit( a ) );
            var argumentsScope = this.GetExpressionScope( transformedArguments, node );
            var transformedInitializer = this.Visit( node.Initializer );
            var initializerScope = this.GetNodeScope( transformedInitializer );

            var combinedScope = objectTypeScope switch
            {
                CompileTimeOnly => CompileTimeOnly,
                RunTimeOnly => RunTimeOnly,
                _ => this.GetExpressionScope(
                    [node.ArgumentList, transformedInitializer],
                    [argumentsScope, initializerScope],
                    node )
            };

            var transformedArgumentList = transformedArguments != null
                ? ArgumentList(
                    node.ArgumentList!.OpenParenToken,
                    SeparatedList( transformedArguments, node.ArgumentList.Arguments.GetSeparators() ),
                    node.ArgumentList!.CloseParenToken )
                : null;

            return node.Update(
                    node.NewKeyword,
                    transformedType,
                    transformedArgumentList,
                    transformedInitializer )
                .AddScopeAnnotation( combinedScope );
        }
    }

    public override SyntaxNode VisitWithExpression( WithExpressionSyntax node )
    {
        // The scope is determined by the expression and the initializer must comply.
        var transformedExpression = this.Visit( node.Expression );
        var expressionScope = this.GetNodeScope( transformedExpression ).GetExpressionValueScope();

        var scopeContext = expressionScope switch
        {
            RunTimeOnly =>
                this._currentScopeContext.RunTimePreferred( "on the right side of a 'with' initializer whose left side is run-time" ),
            CompileTimeOnly => this._currentScopeContext.CompileTimeOnly( "on the right side of a 'with' initializer whose left side is compile-time" ),
            _ => null
        };

        using ( this.WithScopeContext( scopeContext ) )
        {
            var transformedInitializer = this.Visit( node.Initializer );

            var scope = expressionScope == RunTimeOrCompileTime
                ? this.GetExpressionScope(
                    [node.Expression, node.Initializer],
                    [expressionScope, this.GetNodeScope( transformedInitializer )],
                    node )
                : expressionScope;

            return node.Update( transformedExpression, node.WithKeyword, transformedInitializer ).AddScopeAnnotation( scope );
        }
    }

    public override SyntaxNode VisitThrowExpression( ThrowExpressionSyntax node )
    {
        using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( "an expression of a 'throw' expression" ) ) )
        {
            var transformedExpression = this.Visit( node.Expression );

            this.RequireScope( transformedExpression, RunTimeOnly, "a 'throw' expression" );

            return node.Update( node.ThrowKeyword, transformedExpression ).AddScopeAnnotation( RunTimeOnly );
        }
    }

    public override SyntaxNode VisitThrowStatement( ThrowStatementSyntax node )
    {
        using ( this.WithScopeContext( this._currentScopeContext.RunTimePreferred( "an expression of a 'throw' statement" ) ) )
        {
            var transformedExpression = this.Visit( node.Expression )!;

            this.RequireScope( transformedExpression, RunTimeOnly, "a 'throw' statement" );

            return node.Update( node.ThrowKeyword, transformedExpression, node.SemicolonToken ).AddScopeAnnotation( RunTimeOnly );
        }
    }

    public override SyntaxNode VisitTryStatement( TryStatementSyntax node )
    {
        var annotatedBlock = this.Visit( node.Block );

        var annotatedCatches = new CatchClauseSyntax[node.Catches.Count];

        for ( var i = 0; i < node.Catches.Count; i++ )
        {
            var @catch = node.Catches[i];

            using ( this.WithScopeContext( this._currentScopeContext.RunTimeConditional( "catch" ) ) )
            {
                var annotatedCatch = this.Visit( @catch );
                annotatedCatches[i] = annotatedCatch;
            }
        }

        FinallyClauseSyntax? annotatedFinally = null;

        if ( node.Finally != null )
        {
            using ( this.WithScopeContext( this._currentScopeContext.RunTimeConditional( "finally" ) ) )
            {
                annotatedFinally = this.Visit( node.Finally );
            }
        }

        return node.WithBlock( annotatedBlock )
            .WithCatches( List( annotatedCatches ) )
            .WithFinally( annotatedFinally! )
            .AddScopeAnnotation( RunTimeOnly );
    }

    public override SyntaxNode? VisitCatchDeclaration( CatchDeclarationSyntax node )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

        if ( symbol != null )
        {
            this.SetLocalSymbolScope( symbol, RunTimeOnly );
        }

        return base.VisitCatchDeclaration( node );
    }

    public override SyntaxNode VisitTypeOfExpression( TypeOfExpressionSyntax node )
    {
        // The processing of typeof(.) is very specific. It is always represented as a compile-time expression 
        // There is then compile-time-to-run-time conversion logic in the rewriter.
        // The value of typeof is scope-neutral except if the type is run-time only.

        TypeSyntax annotatedType;

        using ( this.WithScopeContext( this._currentScopeContext.RunTimeOrCompileTime( "typeof" ) ) )
        {
            annotatedType = this.Visit( node.Type );
        }

        var typeScope = this.GetNodeScope( annotatedType );

        var typeOfScope = typeScope.GetExpressionValueScope() switch
        {
            CompileTimeOnly => CompileTimeOnly,
            RunTimeOnly => ReferencesTemplateParameter() ? TypeOfTemplateTypeParameter : TypeOfRunTimeType,
            RunTimeOrCompileTime => CompileTimeOnlyReturningBoth,
            _ => throw new AssertionFailedException( $"Unexpected templating scope: {typeScope.GetExpressionExecutionScope()}." )
        };

        return node.WithType( annotatedType ).AddScopeAnnotation( typeOfScope );

        bool ReferencesTemplateParameter()
        {
            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node.Type );

            return symbol != null && this._typeParameterDetectionVisitor.Visit( symbol );
        }
    }

    public override SyntaxNode VisitArrayRankSpecifier( ArrayRankSpecifierSyntax node )
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        var transformedSizes = node.Sizes.SelectAsImmutableArray( syntax => this.Visit( syntax )! );

        var sizeScope = this.GetExpressionScope( transformedSizes, node );

        var arrayRankScope = sizeScope.GetExpressionValueScope() switch
        {
            RunTimeOnly => RunTimeOnly,
            CompileTimeOnly => RunTimeOrCompileTime,
            RunTimeOrCompileTime => RunTimeOrCompileTime,
            _ => throw new AssertionFailedException( $"Unexpected scope: {sizeScope}." )
        };

        var transformedRank =
            node.WithSizes( SeparatedList( transformedSizes ) ).AddScopeAnnotation( arrayRankScope );

        return transformedRank;
    }

    public override SyntaxNode? VisitThisExpression( ThisExpressionSyntax node )
    {
        if ( node.Parent is MemberAccessExpressionSyntax )
        {
            return base.VisitThisExpression( node );
        }
        else
        {
            return node.AddScopeAnnotation( CompileTimeOnly );
        }
    }

    public override SyntaxNode VisitTupleExpression( TupleExpressionSyntax node )
    {
        var transformedElements = node.Arguments.SelectAsImmutableArray( a => this.Visit( a.Expression ) );
        var tupleScope = this.GetExpressionScope( transformedElements, node ).GetExpressionValueScope( true );
        var transformedArguments = new ArgumentSyntax[transformedElements.Length];

        for ( var i = 0; i < transformedElements.Length; i++ )
        {
            transformedArguments[i] = node.Arguments[i].WithExpression( transformedElements[i] );
        }

        return node.Update( node.OpenParenToken, SeparatedList( transformedArguments, node.Arguments.GetSeparators() ), node.CloseParenToken )
            .AddScopeAnnotation( tupleScope );
    }

    public override SyntaxNode VisitInterpolatedStringExpression( InterpolatedStringExpressionSyntax node )
    {
        var transformedContents = new List<InterpolatedStringContentSyntax>( node.Contents.Count );

        foreach ( var content in node.Contents )
        {
            switch ( content )
            {
                case InterpolatedStringTextSyntax text:
                    transformedContents.Add( text );

                    break;

                case InterpolationSyntax interpolation:
                    var transformedExpression = this.Visit( interpolation.Expression );
                    var expressionScope = transformedExpression.GetScopeFromAnnotation().GetValueOrDefault( RunTimeOrCompileTime );
                    var interpolationScope = expressionScope;

                    if ( expressionScope == CompileTimeOnly )
                    {
                        // Implicit toString conversion.
                        interpolationScope = CompileTimeOnlyReturningBoth;
                    }
                    else if ( expressionScope.GetExpressionValueScope() == RunTimeOnly )
                    {
                        interpolationScope = RunTimeOnly;
                    }

                    transformedContents.Add( interpolation.WithExpression( transformedExpression ).AddScopeAnnotation( interpolationScope ) );

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected content kind {content.Kind()} at '{content.GetLocation()}.'" );
            }
        }

        var totalScope = this.GetExpressionScope( transformedContents, node );

        return node.WithContents( List( transformedContents ) ).AddScopeAnnotation( totalScope );
    }

    public override SyntaxNode VisitInitializerExpression( InitializerExpressionSyntax node )
        => base.VisitInitializerExpression( node )!.AddTargetScopeAnnotation( MustFollowParent );

#if ROSLYN_4_8_0_OR_GREATER

    public override SyntaxNode VisitCollectionExpression( CollectionExpressionSyntax node )
    {
        var elements = node.Elements.SelectAsImmutableArray( e => this.Visit( e ).AssertNotNull() );

        var combinedScope = this.GetExpressionScope( elements, node );

        return node.WithElements( SeparatedList( elements ) )
            .AddScopeAnnotation( combinedScope );
    }

#endif

    public override SyntaxNode? VisitDefaultExpression( DefaultExpressionSyntax node )
    {
        if ( this._templateMemberClassifier.IsNodeOfDynamicType( node.Type ) )
        {
            this.ReportDiagnostic(
                TemplatingDiagnosticDescriptors.ForbiddenDynamicUseInTemplate,
                node.Type.GetLocation(),
                default );
        }

        return base.VisitDefaultExpression( node );
    }
}