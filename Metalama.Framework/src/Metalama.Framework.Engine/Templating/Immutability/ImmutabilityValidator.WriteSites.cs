// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Metalama.Framework.Engine.Templating.Immutability
{
    /// <summary>
    /// The rules that verify where a member is written, for the members whose every write can be seen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>readonly</c> is the strongest way to state that a field is set once, because the compiler checks it. It is
    /// not the only way, and demanding it rejects two shapes that are perfectly immutable in practice: a private
    /// field assigned from more than one place in a constructor chain, and a property with a private setter, which is
    /// also what a hand-written <c>ReferenceTypeSerializer</c> needs in order to restore an object after
    /// <c>CreateInstance</c>.
    /// </para>
    /// <para>
    /// So when the write access of a member is private, the declaration is accepted and these rules check the
    /// assignments instead. That is sound precisely because private write access confines every assignment to the
    /// declaring type, which is in this compilation, so every assignment can be seen. Where write access is wider,
    /// the assignments cannot be seen, and the declaration rules report instead.
    /// </para>
    /// <para>
    /// The diagnostic is reported at the assignment rather than at the declaration, because that is the line that has
    /// to change.
    /// </para>
    /// </remarks>
    internal sealed partial class ImmutabilityValidator
    {
        /// <summary>
        /// Reports the writes, in the members and initializers of a type declaration, to the members of a type bound by the
        /// contract.
        /// </summary>
        /// <param name="typeDeclaration">The declaration of the type in the current syntax tree. Nested type declarations
        /// are not examined here, because they are validated on their own.</param>
        /// <remarks>
        /// The operation of a method or constructor declaration includes its body, its expression body and, for a
        /// constructor, its <c>base</c> or <c>this</c> initializer. Field, property and event initializers are examined
        /// separately, because a lambda in an initializer can write a member long after construction.
        /// </remarks>
        private void ValidateWriteSites( SyntaxNode typeDeclaration )
        {
            // The walk stops at bodies and initializers, whose content is examined through their operation instead.
            var nodes = typeDeclaration.DescendantNodes(
                n => n == typeDeclaration
                     || !(SyntaxFacts.IsTypeDeclaration( n.Kind() )
                          || n.Kind() is SyntaxKind.Block or SyntaxKind.ArrowExpressionClause or SyntaxKind.EqualsValueClause
                              or SyntaxKind.BaseConstructorInitializer or SyntaxKind.ThisConstructorInitializer) );

            foreach ( var node in nodes )
            {
                SyntaxNode? root;
                ISymbol? containingSymbol;

                switch ( node.Kind() )
                {
                    case SyntaxKind.MethodDeclaration or SyntaxKind.ConstructorDeclaration or SyntaxKind.DestructorDeclaration
                        or SyntaxKind.OperatorDeclaration or SyntaxKind.ConversionOperatorDeclaration:
                        root = node;
                        containingSymbol = this._semanticModel.GetDeclaredSymbol( node );

                        break;

                    case SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                        or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration:
                        root = node;
                        containingSymbol = this._semanticModel.GetDeclaredSymbol( node );

                        break;

                    case SyntaxKind.PropertyDeclaration when node is PropertyDeclarationSyntax property:
                        // A property can have an expression body and an initializer, but not both.
                        root = (SyntaxNode?) property.ExpressionBody ?? property.Initializer;
                        containingSymbol = property.ExpressionBody != null
                            ? this._semanticModel.GetDeclaredSymbol( property )?.GetMethod
                            : this._semanticModel.GetDeclaredSymbol( property );

                        break;

                    case SyntaxKind.IndexerDeclaration when node is IndexerDeclarationSyntax { ExpressionBody: { } expressionBody } indexer:
                        root = expressionBody;
                        containingSymbol = this._semanticModel.GetDeclaredSymbol( indexer )?.GetMethod;

                        break;

                    // The declarator of a field or of a field-like event, with an initializer.
                    case SyntaxKind.VariableDeclarator
                        when node is VariableDeclaratorSyntax { Initializer: { } initializer, Parent.Parent: FieldDeclarationSyntax or EventFieldDeclarationSyntax }:
                        root = initializer;
                        containingSymbol = this._semanticModel.GetDeclaredSymbol( node );

                        break;

                    default:
                        continue;
                }

                if ( root == null || containingSymbol == null )
                {
                    continue;
                }

                var rootOperation = this._semanticModel.GetOperation( root );

                if ( rootOperation == null )
                {
                    continue;
                }

                foreach ( var operation in rootOperation.DescendantsAndSelf() )
                {
                    this.ValidateWriteSite( operation, containingSymbol );
                }
            }
        }

        private void ValidateWriteSite( IOperation operation, ISymbol containingSymbol )
        {
            switch ( operation )
            {
                // An 'out' argument is a write, and one that is easy to miss because it does not look like an assignment.
                case IArgumentOperation { Parameter.RefKind: RefKind.Out } argument:
                    this.ReportWrite( operation, containingSymbol, argument.Value, ImmutabilityDiagnosticDescriptors.MemberIsPassedByReference );

                    break;

                case IAssignmentOperation assignment when !IsInitializerAssignment( operation ):
                    // A deconstruction assigns every element of a tuple target, so each one has to be examined.
                    if ( operation.Kind == OperationKind.DeconstructionAssignment )
                    {
                        this.ReportDeconstructionTargets( operation, containingSymbol, assignment.Target );
                    }
                    else
                    {
                        this.ReportWrite( operation, containingSymbol, assignment.Target, ImmutabilityDiagnosticDescriptors.MemberIsWrittenOutsideConstructor );
                    }

                    break;

                case IIncrementOrDecrementOperation incrementOrDecrement:
                    this.ReportWrite(
                        operation,
                        containingSymbol,
                        incrementOrDecrement.Target,
                        ImmutabilityDiagnosticDescriptors.MemberIsWrittenOutsideConstructor );

                    break;
            }
        }

        /// <summary>
        /// Reports every member written by the target of a deconstruction, descending through nested tuples.
        /// </summary>
        private void ReportDeconstructionTargets( IOperation operation, ISymbol containingSymbol, IOperation target )
        {
            switch ( target )
            {
                case ITupleOperation tuple:
                    foreach ( var element in tuple.Elements )
                    {
                        this.ReportDeconstructionTargets( operation, containingSymbol, element );
                    }

                    break;

                // A discard writes nothing, and a declaration expression declares a local.
                case IDiscardOperation or IDeclarationExpressionOperation:
                    break;

                default:
                    this.ReportWrite( operation, containingSymbol, target, ImmutabilityDiagnosticDescriptors.MemberIsWrittenOutsideConstructor );

                    break;
            }
        }

        private void ReportWrite(
            IOperation operation,
            ISymbol containingSymbol,
            IOperation target,
            Framework.Diagnostics.DiagnosticDefinition<(string Type, string Member)> definition )
        {
            var member = target switch
            {
                IFieldReferenceOperation field => (ISymbol) field.Field,

                // Only an automatically implemented property, so that these rules examine exactly the members that the
                // declaration rules examine. A property with a body is not state of its own.
                IPropertyReferenceOperation property when property.Property.IsAutoProperty() == true => property.Property,

                _ => null
            };

            if ( member is not { IsStatic: false } || (member.Kind == SymbolKind.Field && member is IFieldSymbol { IsConst: true }) )
            {
                return;
            }

            var declaringType = member.ContainingType;

            if ( declaringType == null || !this._context.IsSubjectToContract( declaringType ) || !this.IsCompileTimeType( declaringType ) )
            {
                return;
            }

            // An advice member is code injected into the target, not state of the aspect, so writing it is not a defect.
            var declaredMember = member.Kind == SymbolKind.Field && member is IFieldSymbol { AssociatedSymbol: { } associated } ? associated : member;

            if ( this.IsAdviceMember( declaredMember ) )
            {
                return;
            }

            if ( IsInConstructorOrInitAccessor( operation, containingSymbol, declaringType ) && IsCurrentInstance( target ) )
            {
                return;
            }

            this._reportDiagnostic(
                definition.CreateRoslynDiagnostic(
                    target.Syntax.GetLocation(),
                    (ImmutabilitySymbolFacts.GetDisplayName( declaringType ), declaredMember.Name) ) );
        }

        /// <summary>
        /// Determines whether an assignment is part of an object initializer or of a <c>with</c> expression.
        /// </summary>
        /// <remarks>
        /// Neither is a mutation. <c>new C { X = 1 }</c> and <c>options with { X = 1 }</c> both assign a freshly
        /// created object, which is exactly what an <c>init</c> accessor exists to allow, and the compiler already
        /// restricts them to that. Without this the rule reports every <c>with</c> expression over an immutable
        /// record, which is the idiomatic way to write one.
        /// </remarks>
        private static bool IsInitializerAssignment( IOperation operation )
            => operation.Parent is IObjectOrCollectionInitializerOperation;

        /// <summary>
        /// Determines whether a member reference reads the instance being constructed rather than another one.
        /// </summary>
        /// <remarks>
        /// A constructor may hold a second instance of its own type, as in <c>C( C other )</c>, and writing a member
        /// of that one mutates an object whose construction is over. Being inside a constructor is therefore not
        /// enough: the receiver has to be the current instance. A static member has no receiver and is written only
        /// where the rules already allow it.
        /// </remarks>
        private static bool IsCurrentInstance( IOperation target )
            => target switch
            {
                IFieldReferenceOperation { Instance: { } instance } => IsThis( instance ),
                IPropertyReferenceOperation { Instance: { } instance } => IsThis( instance ),
                _ => true
            };

        private static bool IsThis( IOperation instance )
            => instance is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };

        /// <summary>
        /// Determines whether an operation is in a place where writing a member of the type is legitimate, that is,
        /// in a constructor or an <c>init</c> accessor of the type that declares it.
        /// </summary>
        /// <remarks>
        /// A lambda or a local function is excluded even when it is written inside a constructor, because it does not
        /// run there: the delegate may be stored and invoked at any later time, so an assignment in its body is not
        /// part of construction. Field and property initializers are not assignments and never reach this rule.
        /// </remarks>
        private static bool IsInConstructorOrInitAccessor( IOperation operation, ISymbol containingSymbol, INamedTypeSymbol declaringType )
        {
            for ( var current = operation.Parent; current != null; current = current.Parent )
            {
                if ( current is IAnonymousFunctionOperation or ILocalFunctionOperation )
                {
                    return false;
                }
            }

            return containingSymbol.Kind == SymbolKind.Method
                   && containingSymbol is IMethodSymbol method
                   && SymbolEqualityComparer.Default.Equals( method.ContainingType, declaringType )
                   && (method.MethodKind == MethodKind.Constructor
                       || (method.MethodKind == MethodKind.PropertySet && method.IsInitOnly));
        }
    }
}
