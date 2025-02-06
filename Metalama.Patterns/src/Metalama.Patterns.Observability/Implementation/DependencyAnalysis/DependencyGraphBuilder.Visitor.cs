// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;
using RefKind = Microsoft.CodeAnalysis.RefKind;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

internal partial class DependencyGraphBuilder
{
    private static void AddReferencedProperties(
        ICompilation compilation,
        ObservablePropertyInfo propertyInfo,
        GraphBuildingContext context,
        Action<string>? trace = null,
        CancellationToken cancellationToken = default )
    {
        var symbol = propertyInfo.FieldOrProperty.GetSymbol()!;

        var body = symbol
            .DeclaringSyntaxReferences
            .Select( r => r.GetSyntax( cancellationToken ) )
            .Cast<PropertyDeclarationSyntax>()
            .Select( RoslynExtensions.GetGetterBody )
            .SingleOrDefault();

        if ( body == null )
        {
            return;
        }

        var ignoreWarnings = context.CanIgnoreUnobservableExpressions( symbol );

        // ReSharper disable once InvokeAsExtensionMethod
        var semanticModel = SymbolExtensions.GetSemanticModel( compilation, body.SyntaxTree );

        var visitor = new Visitor( propertyInfo, semanticModel, context, trace, ignoreWarnings, cancellationToken );

        visitor.Visit( body );
    }

    [CompileTime]
    private sealed partial class Visitor : CSharpSyntaxWalker, IGatherIdentifiersContextManagerClient
    {
        private readonly ObservablePropertyInfo _propertyInfo;
        private readonly INamedType _declaringType;
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<string>? _trace;
        private readonly GraphBuildingContext _context;
        private readonly ICompilation _compilation;
        private readonly GatherIdentifiersContextManager _gatherManager;
        private readonly bool _ignoreWarnings;

        private int _depth = 1;

        public Visitor(
            ObservablePropertyInfo propertyInfo,
            SemanticModel semanticModel,
            GraphBuildingContext context,
            Action<string>? trace,
            bool ignoreWarnings,
            CancellationToken cancellationToken )
        {
            this._trace = trace;
            this._propertyInfo = propertyInfo;
            this._declaringType = propertyInfo.FieldOrProperty.DeclaringType;
            this._semanticModel = semanticModel;
            this._cancellationToken = cancellationToken;
            this._ignoreWarnings = ignoreWarnings;
            this._context = context;
            this._gatherManager = new GatherIdentifiersContextManager( this );
            this._compilation = this._declaringType.Compilation;
        }

        private IFieldOrProperty GetFieldOrProperty( ISymbol symbol )
        {
            return (IFieldOrProperty) this._compilation.GetDeclaration( symbol );
        }

        void IGatherIdentifiersContextManagerClient.OnRootContextPopped( GatherIdentifiersContext context )
        {
            this.ProcessAndResetIfApplicable( context );
        }

        private bool IsLocalInstanceMember( ISymbol symbol ) => !symbol.IsStatic && this._declaringType.GetSymbol()!.IsOrInheritsFrom( symbol.ContainingType );

        private void ValidatePathElement( IReadOnlyList<DependencyPathElement> symbols, int index, ChainSection chainSection )
        {
            if ( this._ignoreWarnings )
            {
                return;
            }

            // Here we report particularly those diagnostics which should be located on a syntax node inside a body. The final dependency
            // graph does not keep track of all the syntax nodes which referenced each dependency node (this would be expensive), so logging
            // diagnostics located on syntax nodes inside bodies is not possible later.

            var pathElement = symbols[index];

            var fieldOrPropertyType = pathElement.Symbol switch
            {
                IPropertySymbol property => property.Type.GetElementaryType(),
                IFieldSymbol field => field.Type.GetElementaryType(),
                _ => chainSection == ChainSection.Unsupported ? null : throw new ObservabilityAssertionFailedException()
            };

            if ( chainSection == ChainSection.Stem )
            {
                // Warn if this is a non-leaf reference to a non-primitive or non-INPC type.

                if ( !this._context.IsDeeplyImmutable( fieldOrPropertyType! ) && !this._context.TreatAsImplementingInpc( fieldOrPropertyType! ) )
                {
                    this._context.ReportDiagnostic(
                        DiagnosticDescriptors.WarningChildrenOfNonInpcFieldsOrPropertiesAreNotObservable.WithArguments( fieldOrPropertyType! ),
                        pathElement.Node.GetLocation() );
                }

                // Warn if this is a non-leaf reference to an INPC non-auto property of the target type because we can't
                // (yet) track changes to children of indirectly-referenced INPC properties.

                if ( !this._context.IsAutoPropertyOrField( pathElement.Symbol )
                     && this._context.TreatAsImplementingInpc( fieldOrPropertyType! )
                     && pathElement.Symbol.ContainingType.Equals( this._declaringType.GetSymbol() ) )
                {
                    this._context.ReportDiagnostic(
                        DiagnosticDescriptors.DeclaringTypeDoesNotImplementInpc
                            .WithArguments( (pathElement.Symbol, pathElement.Symbol.ContainingType) ),
                        symbols[index + 1].Node.GetLocation() );
                }
            }

            if ( pathElement.Symbol is IFieldSymbol fieldSymbol )
            {
                var isSafe = fieldSymbol.IsConst ||
                             (!fieldSymbol.IsStatic && fieldSymbol.GetEffectiveAccessibility() == Accessibility.Private) ||
                             (fieldSymbol.IsReadOnly && this._context.IsDeeplyImmutable( fieldSymbol.Type ));

                if ( !isSafe )
                {
                    this._context.ReportDiagnostic(
                        DiagnosticDescriptors.NonPrivateFieldsNonSupported
                            .WithArguments( fieldSymbol ),
                        pathElement.Node.GetLocation() );
                }
            }
            else if ( pathElement.Symbol.Kind is SymbolKind.Method )
            {
                var method = (IMethodSymbol) pathElement.Symbol;

                var isSafe =

                    // Methods marked as constant are safe.
                    this._context.IsConstant( method ) ||

                    // Methods that have only immutable arguments are safe.
                    ((method.IsStatic || this._context.IsDeeplyImmutable( method.ContainingType ))
                     && method.Parameters.All( p => p.RefKind is RefKind.Out || this._context.IsDeeplyImmutable( p.Type ) )) ||

                    // All methods that have no output are safe.
                    (method.ReturnsVoid && !method.Parameters.Any( p => p.RefKind is RefKind.Out or RefKind.Ref ));

                if ( !isSafe )
                {
                    this._context.ReportDiagnostic(
                        DiagnosticDescriptors.WarningMethodOrPropertyIsNotSupportedForDependencyAnalysis.WithArguments(
                            (pathElement.Symbol.Kind, pathElement.Symbol) ),
                        pathElement.Node.GetLocation() );
                }
            }
        }

        private void ProcessAndResetIfApplicable( GatherIdentifiersContext context )
        {
            this._trace?.Invoke( ">> ProcessAndResetIfApplicable" );

            if ( context.StartDepth == this._depth )
            {
                foreach ( var symbols in context.SymbolsForAllForks() )
                {
                    this._trace?.Invoke( $"Processing symbol chain {string.Join( ".", symbols.Select( sr => sr.Symbol.Name ) )}" );

                    // Skip chains which are not read, but are only the target of an assignment.

                    if ( symbols[^1].Node.GetAccessKind() is not (AccessKind.Read or AccessKind.ReadWrite) )
                    {
                        continue;
                    }

                    // Only consider chains that start with a local member. And for now, only fields and properties.
                    // TODO: Expand logic here to support methods.

                    var firstSymbol = symbols[0].Symbol;

                    var supportedStemAndLeafCount = this.IsLocalInstanceMember( firstSymbol )
                        ? symbols.TakeWhile(
                                sr => sr.Symbol.Kind == SymbolKind.Property
                                      || (sr.Symbol.Kind == SymbolKind.Field && sr.Symbol.GetEffectiveAccessibility() == Accessibility.Private) )
                            .Count()
                        : 0;

                    ObservableExpression? reference = null;

                    for ( var i = 0; i < symbols.Count; ++i )
                    {
                        var chainSection =
                            i < supportedStemAndLeafCount - 1
                                ? ChainSection.Stem
                                : i < supportedStemAndLeafCount
                                    ? ChainSection.Leaf
                                    : ChainSection.Unsupported;

                        this.ValidatePathElement( symbols, i, chainSection );

                        if ( chainSection is ChainSection.Stem or ChainSection.Leaf )
                        {
                            var referencedProperty =
                                this._propertyInfo.DeclaringTypeInfo.Builder.GetOrAddPropertyNode( this.GetFieldOrProperty( symbols[i].Symbol ) );

                            reference = reference == null
                                ? this._propertyInfo.DeclaringTypeInfo.GetOrAddProperty( this.GetFieldOrProperty( firstSymbol ) ).RootReferenceNode
                                : reference.GetOrAddChildReference( referencedProperty );
                        }
                    }

                    if ( reference != null && reference.ReferencedFieldOrProperty != this._propertyInfo.FieldOrProperty )
                    {
                        reference.AddLeafReferencingProperty( this._propertyInfo );
                    }
                }

                context.Reset();
            }

            this._trace?.Invoke( "<< ProcessAndResetIfApplicable" );
        }

        public override void Visit( SyntaxNode? node )
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            ++this._depth;

            base.Visit( node );

            this.ProcessAndResetIfApplicable( this._gatherManager.Current );

            --this._depth;
        }

        /*
        public override void VisitInvocationExpression( InvocationExpressionSyntax node )
        {
            foreach ( var arg in node.ArgumentList.Arguments )
            {
                this.ValidateMethodArgumentType( arg.Expression );
            }

            base.VisitInvocationExpression( node );
        }
        */

        public override void VisitArgument( ArgumentSyntax node )
        {
            using ( this._gatherManager.UseNewRootContext() )
            {
                base.VisitArgument( node );
            }
        }

        public override void VisitVariableDeclaration( VariableDeclarationSyntax node )
        {
            var symbolInfo = this._semanticModel.GetSymbolInfo( node.Type, this._cancellationToken );
            var variableType = ((ITypeSymbol?) symbolInfo.Symbol)?.GetElementaryType();

            if ( variableType != null && !this._context.IsDeeplyImmutable( variableType ) && !this._ignoreWarnings )
            {
                foreach ( var variable in node.Variables )
                {
                    this._context.ReportDiagnostic(
                        DiagnosticDescriptors.LocalVariablesNonSupported.WithArguments( symbolInfo.Symbol! ),
                        variable.Identifier.GetLocation() );
                }
            }

            base.VisitVariableDeclaration( node );
        }

        public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
        {
            // TODO: For now we don't try to examine the body of local functions - much as we don't examine the body of regular methods.
            // Invocations of the local function will warn if they are not supported (primitive arg types only etc), but as with regular
            // methods, don't warn on the declaration.
        }

        public override void VisitConditionalExpression( ConditionalExpressionSyntax node )
        {
            // `Condition` is an isolated expression, use a new root context while visiting it.
            using ( this._gatherManager.UseNewRootContext() )
            {
                base.Visit( node.Condition );
            }

            var current = this._gatherManager.Current;
            current.EnsureStarted( this._depth );

            var forkWhenFalse = current.PrepareFork( this._depth );

            // Visit `WhenTrue` using the current context.
            this.Visit( node.WhenTrue );

            // Visit `WhenFalse` using the prepared fork.
            using ( forkWhenFalse.Use() )
            {
                this.Visit( node.WhenFalse );
            }
        }

        public override void VisitBinaryExpression( BinaryExpressionSyntax node )
        {
            if ( node.IsKind( SyntaxKind.CoalesceExpression ) )
            {
                var current = this._gatherManager.Current;
                current.EnsureStarted( this._depth );

                var forkRight = current.PrepareFork( this._depth );

                // Visit `Left` using the current context.
                this.Visit( node.Left );

                // Visit `Right` using the prepared fork.
                using ( forkRight.Use() )
                {
                    this.Visit( node.Right );
                }
            }
            else
            {
                // Process each operand in its own context.

                using ( this._gatherManager.UseNewRootContext() )
                {
                    this.Visit( node.Left );
                }

                using ( this._gatherManager.UseNewRootContext() )
                {
                    this.Visit( node.Right );
                }
            }
        }

        public override void VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
        {
            this._gatherManager.Current.EnsureStarted( this._depth );

            base.VisitMemberAccessExpression( node );
        }

        public override void VisitConditionalAccessExpression( ConditionalAccessExpressionSyntax node )
        {
            this._gatherManager.Current.EnsureStarted( this._depth );

            base.VisitConditionalAccessExpression( node );
        }

        public override void VisitIdentifierName( IdentifierNameSyntax node )
        {
            var ctx = this._gatherManager.Current;

            var symbol = this._semanticModel.GetSymbolInfo( node, this._cancellationToken ).Symbol;

            if ( symbol != null )
            {
                if ( ctx.StartDepth == 0 )
                {
                    // This happens when an identifier is used without qualification,
                    // eg. X in `int Y => X`
                    if ( node.GetAccessKind() is AccessKind.Read or AccessKind.ReadWrite )
                    {
                        ctx.EnsureStarted( this._depth );
                    }
                }

                if ( ctx.StartDepth > 0 )
                {
                    ctx.AddSymbol( symbol, node );
                }
            }
            else
            {
                // Undefined name, will be a compiler error (eg, "The name 'X' does not exist in the current context").
                ctx.Reset();
            }
        }
    }
}