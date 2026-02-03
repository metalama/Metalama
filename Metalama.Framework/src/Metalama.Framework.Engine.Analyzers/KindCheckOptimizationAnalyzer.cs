// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Analyzers;

/// <summary>
/// Analyzer to detect pattern matching on IDeclaration, ISymbol, and SyntaxNode subtypes
/// without a preceding Kind discriminator check, which is a performance anti-pattern.
/// </summary>
[DiagnosticAnalyzer( LanguageNames.CSharp )]
[UsedImplicitly]
public class KindCheckOptimizationAnalyzer : DiagnosticAnalyzer
{
    // Range: 0860-0869
    internal static readonly DiagnosticDescriptor PatternMatchingWithoutKindCheck = new(
        "LAMA0860",
        "Pattern matching without Kind check",
        "Pattern matching on '{0}' should be preceded by a Kind check for better performance",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( PatternMatchingWithoutKindCheck );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction( InitializeCompilation );
    }

    private static void InitializeCompilation( CompilationStartAnalysisContext context )
    {
        // Cache base type symbols
        var iDeclarationSymbol = context.Compilation.GetTypeByMetadataName( "Metalama.Framework.Code.IDeclaration" );
        var iSymbolSymbol = context.Compilation.GetTypeByMetadataName( "Microsoft.CodeAnalysis.ISymbol" );
        var syntaxNodeSymbol = context.Compilation.GetTypeByMetadataName( "Microsoft.CodeAnalysis.SyntaxNode" );

        if ( iDeclarationSymbol == null && iSymbolSymbol == null && syntaxNodeSymbol == null )
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeIsPattern( ctx, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ),
            SyntaxKind.IsPatternExpression );

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeSwitchStatement( ctx, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ),
            SyntaxKind.SwitchStatement );

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeSwitchExpression( ctx, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ),
            SyntaxKind.SwitchExpression );
    }

    private static void AnalyzeIsPattern(
        SyntaxNodeAnalysisContext ctx,
        INamedTypeSymbol? iDeclarationSymbol,
        INamedTypeSymbol? iSymbolSymbol,
        INamedTypeSymbol? syntaxNodeSymbol )
    {
        var isPattern = (IsPatternExpressionSyntax) ctx.Node;

        // Extract the type from the pattern
        var patternType = isPattern.Pattern switch
        {
            DeclarationPatternSyntax decl => decl.Type,
            RecursivePatternSyntax { Type: not null } rec => rec.Type,
            _ => null
        };

        if ( patternType == null )
        {
            return;
        }

        // Get the type symbol and check if it's IDeclaration/ISymbol/SyntaxNode subtype
        var typeSymbol = ctx.SemanticModel.GetTypeInfo( patternType ).Type;

        if ( !IsRelevantType( typeSymbol, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
        {
            return;
        }

        // Check the type of the expression being pattern-matched.
        // Only flag if the expression type is ISymbol, IDeclaration, or SyntaxNode.
        // If it's object or some other type, pattern matching is the appropriate approach.
        var expressionType = ctx.SemanticModel.GetTypeInfo( isPattern.Expression ).Type;

        if ( !IsRelevantBaseType( expressionType, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
        {
            return;
        }

        // Check if there's a preceding Kind check in the same && chain
        if ( HasPrecedingKindCheck( isPattern ) )
        {
            return;
        }

        // Check if the is-pattern is inside a when clause of a switch that already has a Kind check
        if ( IsInsideSwitchWithKindCheck( isPattern ) )
        {
            return;
        }

        // Report diagnostic
        ctx.ReportDiagnostic(
            Diagnostic.Create(
                PatternMatchingWithoutKindCheck,
                patternType.GetLocation(),
                typeSymbol!.Name ) );
    }

    private static void AnalyzeSwitchStatement(
        SyntaxNodeAnalysisContext ctx,
        INamedTypeSymbol? iDeclarationSymbol,
        INamedTypeSymbol? iSymbolSymbol,
        INamedTypeSymbol? syntaxNodeSymbol )
    {
        var switchStmt = (SwitchStatementSyntax) ctx.Node;

        // Check if governing expression is already a Kind access
        if ( IsKindAccess( switchStmt.Expression ) )
        {
            return;
        }

        // Get the type of the governing expression
        var governingType = ctx.SemanticModel.GetTypeInfo( switchStmt.Expression ).Type;

        if ( !IsRelevantBaseType( governingType, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
        {
            return;
        }

        // Iterate through each switch section
        foreach ( var section in switchStmt.Sections )
        {
            foreach ( var label in section.Labels )
            {
                // Check for CasePatternSwitchLabelSyntax with type pattern
                if ( label is CasePatternSwitchLabelSyntax { Pattern: var pattern } )
                {
                    var patternType = pattern switch
                    {
                        DeclarationPatternSyntax decl => decl.Type,
                        RecursivePatternSyntax { Type: not null } rec => rec.Type,
                        _ => null
                    };

                    if ( patternType == null )
                    {
                        continue;
                    }

                    var typeSymbol = ctx.SemanticModel.GetTypeInfo( patternType ).Type;

                    if ( IsRelevantType( typeSymbol, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
                    {
                        // Report diagnostic on each type pattern
                        ctx.ReportDiagnostic(
                            Diagnostic.Create(
                                PatternMatchingWithoutKindCheck,
                                patternType.GetLocation(),
                                typeSymbol!.Name ) );
                    }
                }
            }
        }
    }

    private static void AnalyzeSwitchExpression(
        SyntaxNodeAnalysisContext ctx,
        INamedTypeSymbol? iDeclarationSymbol,
        INamedTypeSymbol? iSymbolSymbol,
        INamedTypeSymbol? syntaxNodeSymbol )
    {
        var switchExpr = (SwitchExpressionSyntax) ctx.Node;

        // Check if governing expression is already a Kind access
        if ( IsKindAccess( switchExpr.GoverningExpression ) )
        {
            return;
        }

        // Get the type of the governing expression
        var governingType = ctx.SemanticModel.GetTypeInfo( switchExpr.GoverningExpression ).Type;

        if ( !IsRelevantBaseType( governingType, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
        {
            return;
        }

        // Iterate through each arm
        foreach ( var arm in switchExpr.Arms )
        {
            var patternType = arm.Pattern switch
            {
                DeclarationPatternSyntax decl => decl.Type,
                RecursivePatternSyntax { Type: not null } rec => rec.Type,
                _ => null
            };

            if ( patternType == null )
            {
                continue;
            }

            var typeSymbol = ctx.SemanticModel.GetTypeInfo( patternType ).Type;

            if ( IsRelevantType( typeSymbol, iDeclarationSymbol, iSymbolSymbol, syntaxNodeSymbol ) )
            {
                // Report diagnostic on each type pattern
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        PatternMatchingWithoutKindCheck,
                        patternType.GetLocation(),
                        typeSymbol!.Name ) );
            }
        }
    }

    private static bool IsKindAccess( ExpressionSyntax expression )
    {
        // Check for property access: x.Kind, x.DeclarationKind, x.TypeKind
        if ( expression is MemberAccessExpressionSyntax memberAccess )
        {
            var name = memberAccess.Name.Identifier.Text;

            return name is "Kind" or "DeclarationKind" or "TypeKind";
        }

        // Check for conditional access: x?.Kind, x?.DeclarationKind, x?.TypeKind
        if ( expression is ConditionalAccessExpressionSyntax conditionalAccess )
        {
            // Handle x?.Kind property access
            if ( conditionalAccess.WhenNotNull is MemberBindingExpressionSyntax memberBinding )
            {
                return memberBinding.Name.Identifier.Text is "Kind" or "DeclarationKind" or "TypeKind";
            }

            // Handle x?.Kind() method invocation
            if ( conditionalAccess.WhenNotNull is InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax invokeBinding } )
            {
                return invokeBinding.Name.Identifier.Text == "Kind";
            }
        }

        // Check for method invocation: node.Kind()
        if ( expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax invokeMember } )
        {
            return invokeMember.Name.Identifier.Text == "Kind";
        }

        // Check for tuple expression containing Kind accesses: (left.Kind, right.Kind)
        if ( expression is TupleExpressionSyntax tupleExpr )
        {
            return tupleExpr.Arguments.Any( arg => IsKindAccess( arg.Expression ) );
        }

        // Check for parenthesized expression
        if ( expression is ParenthesizedExpressionSyntax parenExpr )
        {
            return IsKindAccess( parenExpr.Expression );
        }

        return false;
    }

    private static bool IsInsideSwitchWithKindCheck( IsPatternExpressionSyntax isPattern )
    {
        // Walk up the syntax tree to find if we're inside a when clause of a switch
        // that already has a Kind check in its governing expression or case pattern
        var current = isPattern.Parent;

        while ( current != null )
        {
            // Check if we're in a switch expression arm (handles both direct and when clause cases)
            if ( current is SwitchExpressionArmSyntax arm )
            {
                // Find the switch expression
                var switchExpr = arm.Parent as SwitchExpressionSyntax;

                if ( switchExpr != null )
                {
                    // If the switch is on Kind access, check if arm has a Kind enum pattern
                    if ( IsKindAccess( switchExpr.GoverningExpression ) && IsKindEnumPattern( arm.Pattern ) )
                    {
                        return true;
                    }

                    // Also check if the arm pattern itself contains a Kind check (property pattern)
                    if ( PatternContainsKindCheck( arm.Pattern ) )
                    {
                        return true;
                    }
                }

                // Don't return false here - continue walking up in case we're nested in another switch
                current = current.Parent;

                continue;
            }

            // Check if we're in a when clause of a switch statement case
            if ( current is CasePatternSwitchLabelSyntax caseLabel )
            {
                // Find the switch statement
                var caseLabelSection = caseLabel.Parent;
                var switchStatement = caseLabelSection?.Parent as SwitchStatementSyntax;

                if ( switchStatement != null )
                {
                    // If the switch is on Kind access, check if case has a Kind enum pattern
                    if ( IsKindAccess( switchStatement.Expression ) && IsKindEnumPattern( caseLabel.Pattern ) )
                    {
                        return true;
                    }

                    // Also check if the case pattern itself contains a Kind check (property pattern)
                    if ( PatternContainsKindCheck( caseLabel.Pattern ) )
                    {
                        return true;
                    }
                }

                // Don't return false here - continue walking up in case we're nested
                current = current.Parent;

                continue;
            }

            // Check if we're inside a switch section (switch statement body)
            if ( current is SwitchSectionSyntax switchSection )
            {
                var switchStatement = switchSection.Parent as SwitchStatementSyntax;

                if ( switchStatement != null && IsKindAccess( switchStatement.Expression ) )
                {
                    // Check if any label in this section has a Kind enum pattern
                    foreach ( var label in switchSection.Labels )
                    {
                        if ( label is CasePatternSwitchLabelSyntax patternLabel && IsKindEnumPattern( patternLabel.Pattern ) )
                        {
                            return true;
                        }

                        if ( label is CaseSwitchLabelSyntax )
                        {
                            // Simple case label (e.g., case SyntaxKind.X:) - the switch is already on Kind
                            return true;
                        }
                    }
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsKindEnumPattern( PatternSyntax pattern )
    {
        // Check for constant pattern (e.g., SymbolKind.Method or DeclarationKind.Method)
        if ( pattern is ConstantPatternSyntax )
        {
            return true;
        }

        // Check for "or" pattern combining constant patterns (e.g., SyntaxKind.X or SyntaxKind.Y)
        if ( pattern is BinaryPatternSyntax binaryPattern )
        {
            return IsKindEnumPattern( binaryPattern.Left ) || IsKindEnumPattern( binaryPattern.Right );
        }

        // Check for tuple pattern with constant patterns (e.g., (SymbolKind.Method, SymbolKind.Method))
        if ( pattern is RecursivePatternSyntax { PositionalPatternClause: { } positionalClause } )
        {
            return positionalClause.Subpatterns.Any( sp => IsKindEnumPattern( sp.Pattern ) );
        }

        return false;
    }

    private static bool HasPrecedingKindCheck( IsPatternExpressionSyntax isPattern )
    {
        // Walk up the syntax tree to find the containing binary expression (if any)
        var current = isPattern.Parent;

        while ( current != null )
        {
            if ( current is BinaryExpressionSyntax binaryExpr &&
                 binaryExpr.IsKind( SyntaxKind.LogicalAndExpression ) )
            {
                // Check if the left side of the && contains a Kind check
                if ( ContainsKindCheck( binaryExpr.Left ) )
                {
                    return true;
                }

                // Continue checking parent && expressions
                current = current.Parent;
            }
            else if ( current is ParenthesizedExpressionSyntax )
            {
                current = current.Parent;
            }
            else
            {
                break;
            }
        }

        return false;
    }

    private static bool ContainsKindCheck( ExpressionSyntax expression )
    {
        // Check if the expression is or contains a Kind comparison
        // e.g., x.Kind == SymbolKind.Method or x.DeclarationKind == DeclarationKind.Method
        // or x.DeclarationKind is DeclarationKind.Method

        if ( expression is BinaryExpressionSyntax binaryExpr )
        {
            // Check equality comparisons
            if ( binaryExpr.IsKind( SyntaxKind.EqualsExpression ) || binaryExpr.IsKind( SyntaxKind.NotEqualsExpression ) )
            {
                if ( IsKindAccess( binaryExpr.Left ) || IsKindAccess( binaryExpr.Right ) )
                {
                    return true;
                }
            }

            // Check nested && expressions (left side)
            if ( binaryExpr.IsKind( SyntaxKind.LogicalAndExpression ) )
            {
                return ContainsKindCheck( binaryExpr.Left ) || ContainsKindCheck( binaryExpr.Right );
            }
        }
        else if ( expression is IsPatternExpressionSyntax isPattern )
        {
            // Check for x.DeclarationKind is DeclarationKind.Method or x.Kind is SymbolKind.Method
            if ( IsKindAccess( isPattern.Expression ) )
            {
                return true;
            }

            // Check for x is IType { Kind: SymbolKind.X } property pattern
            if ( PatternContainsKindCheck( isPattern.Pattern ) )
            {
                return true;
            }
        }
        else if ( expression is ParenthesizedExpressionSyntax parenExpr )
        {
            return ContainsKindCheck( parenExpr.Expression );
        }
        else if ( expression is PrefixUnaryExpressionSyntax { Operand: var operand } && expression.IsKind( SyntaxKind.LogicalNotExpression ) )
        {
            return ContainsKindCheck( operand );
        }
        else if ( expression is InvocationExpressionSyntax invocation )
        {
            // Check for Kind-based method calls like x.Kind().IsAccessorDeclaration() or x.DeclarationKind.IsMethod()
            if ( invocation.Expression is MemberAccessExpressionSyntax memberAccess )
            {
                // Check if calling a method on Kind() result, e.g., node.Kind().IsXxx()
                if ( IsKindAccess( memberAccess.Expression ) )
                {
                    return true;
                }

                // Check if the method name starts with "Is" and is called on a Kind-returning expression
                // e.g., declarationKind.IsMethod() where declarationKind is DeclarationKind
                var methodName = memberAccess.Name.Identifier.Text;

                if ( methodName.StartsWith( "Is", System.StringComparison.Ordinal ) && IsKindAccess( memberAccess.Expression ) )
                {
                    return true;
                }

                // Check for x.IsKind(SyntaxKind.X) pattern - common for SyntaxNode
                if ( methodName == "IsKind" && invocation.ArgumentList.Arguments.Count > 0 )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool PatternContainsKindCheck( PatternSyntax pattern )
    {
        // Check for property patterns like { Kind: X } or { DeclarationKind: X } or { TypeKind: X }
        if ( pattern is RecursivePatternSyntax recursivePattern )
        {
            if ( recursivePattern.PropertyPatternClause is { } propertyClause )
            {
                foreach ( var subpattern in propertyClause.Subpatterns )
                {
                    // Check if this is a Kind property check
                    var propertyName = subpattern.NameColon?.Name.Identifier.Text;

                    if ( propertyName is "Kind" or "DeclarationKind" or "TypeKind" )
                    {
                        return true;
                    }
                }
            }

            // Also check nested patterns
            if ( recursivePattern.PropertyPatternClause is { } props )
            {
                foreach ( var subpattern in props.Subpatterns )
                {
                    if ( subpattern.Pattern != null && PatternContainsKindCheck( subpattern.Pattern ) )
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsRelevantType(
        ITypeSymbol? type,
        INamedTypeSymbol? iDeclaration,
        INamedTypeSymbol? iSymbol,
        INamedTypeSymbol? syntaxNode )
    {
        if ( type == null )
        {
            return false;
        }

        // Only flag interface types - concrete classes don't benefit from Kind optimization
        // because we're checking for a specific implementation, not polymorphic subtypes
        if ( type.TypeKind != TypeKind.Interface )
        {
            // Exception: SyntaxNode subtypes (which are classes) do benefit from Kind() checks
            if ( syntaxNode != null )
            {
                var baseType = type.BaseType;

                while ( baseType != null )
                {
                    if ( SymbolEqualityComparer.Default.Equals( baseType, syntaxNode ) )
                    {
                        return true;
                    }

                    baseType = baseType.BaseType;
                }
            }

            return false;
        }

        // Check if type implements IDeclaration
        if ( iDeclaration != null && type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, iDeclaration ) ) )
        {
            return true;
        }

        // Check if type implements ISymbol
        if ( iSymbol != null && type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, iSymbol ) ) )
        {
            return true;
        }

        return false;
    }

    private static bool IsRelevantBaseType(
        ITypeSymbol? type,
        INamedTypeSymbol? iDeclaration,
        INamedTypeSymbol? iSymbol,
        INamedTypeSymbol? syntaxNode )
    {
        if ( type == null )
        {
            return false;
        }

        // Check if type is or implements IDeclaration
        if ( iDeclaration != null )
        {
            if ( SymbolEqualityComparer.Default.Equals( type, iDeclaration ) ||
                 type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, iDeclaration ) ) )
            {
                return true;
            }
        }

        // Check if type is or implements ISymbol
        if ( iSymbol != null )
        {
            if ( SymbolEqualityComparer.Default.Equals( type, iSymbol ) ||
                 type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, iSymbol ) ) )
            {
                return true;
            }
        }

        // Check if type is or derives from SyntaxNode
        if ( syntaxNode != null )
        {
            var currentType = type;

            while ( currentType != null )
            {
                if ( SymbolEqualityComparer.Default.Equals( currentType, syntaxNode ) )
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }
        }

        return false;
    }
}
