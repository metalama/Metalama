// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Provides factory methods to create <see cref="IStatement"/> objects, which are compile-time representations
/// of run-time C# statements.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="StatementFactory"/> class provides multiple approaches to creating <see cref="IStatement"/> objects:
/// <list type="bullet">
/// <item><see cref="Parse"/> - Parse C# statement strings into <see cref="IStatement"/> objects</item>
/// <item><see cref="FromExpression"/> - Create expression statements from <see cref="IExpression"/> objects</item>
/// <item><see cref="FromTemplate(TemplateInvocation, object)"/> and overloads - Create statements by invoking templates</item>
/// <item><see cref="Block(IStatement[])"/> and <see cref="List"/> - Create blocks and statement lists</item>
/// </list>
/// For complex statements that need programmatic construction with indentation, use <see cref="StatementBuilder"/> instead,
/// which provides a StringBuilder-like API with automatic indentation management.
/// </para>
/// </remarks>
/// <seealso cref="IStatement"/>
/// <seealso cref="StatementBuilder"/>
/// <seealso cref="IStatementBuilder"/>
/// <seealso href="@run-time-statements"/>
/// <seealso href="@templates"/>
[CompileTime]
public static class StatementFactory
{
    /// <summary>
    /// Parses a string containing a C# statement and returns an <see cref="IStatement"/>, which can be inserted into run-time code
    /// using <see cref="meta.InsertStatement(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/>.
    /// </summary>
    /// <param name="code">A string containing a single C# statement, which must end with a semicolon or closing bracket.</param>
    /// <returns>An <see cref="IStatement"/> representing the parsed statement.</returns>
    /// <remarks>
    /// This method is useful for simple statements where the content is known as a string. For more complex or programmatically
    /// constructed statements, use <see cref="StatementBuilder"/> instead.
    /// </remarks>
    /// <seealso cref="StatementBuilder"/>
    /// <seealso href="@templates"/>
    public static IStatement Parse( string code ) => SyntaxBuilder.CurrentImplementation.ParseStatement( code );

    /// <summary>
    /// Creates an expression statement from an <see cref="IExpression"/> (e.g., a method invocation or assignment).
    /// </summary>
    /// <param name="expression">The expression to convert into a statement.</param>
    /// <returns>An <see cref="IStatement"/> that executes the expression.</returns>
    /// <remarks>
    /// Expression statements are statements that evaluate an expression for its side effects, such as method calls,
    /// assignments, increment/decrement operations, or object creation. The generated statement will include the
    /// terminating semicolon.
    /// </remarks>
    public static IStatement FromExpression( IExpression expression ) => SyntaxBuilder.CurrentImplementation.CreateExpressionStatement( expression );

    /// <summary>
    /// Creates an <see cref="IStatement"/> by invoking a template specified by a <see cref="TemplateInvocation"/>.
    /// </summary>
    /// <param name="templateInvocation">Specifies which template to invoke.</param>
    /// <param name="args">Optional arguments to pass to the template.</param>
    /// <returns>An <see cref="IStatement"/> that represents the template invocation.</returns>
    /// <remarks>
    /// This method allows you to invoke a template method and use the result as a statement. The template will be expanded
    /// when the aspect is applied, generating the actual C# code at that point.
    /// </remarks>
    /// <seealso cref="TemplateInvocation"/>
    public static IStatement FromTemplate( TemplateInvocation templateInvocation, object? args = null )
        => SyntaxBuilder.CurrentImplementation.CreateTemplateStatement( templateInvocation, args );

    /// <summary>
    /// Creates an <see cref="IStatement"/> obtained by invoking a template specified by its name and optional arguments.
    /// </summary>
    public static IStatement FromTemplate( string templateName, object? args = null )
        => SyntaxBuilder.CurrentImplementation.CreateTemplateStatement( new TemplateInvocation( templateName ), args );

    /// <summary>
    /// Creates an <see cref="IStatement"/> obtained by invoking a template specified by its name, an <see cref="ITemplateProvider"/>, and optional arguments.
    /// </summary>
    public static IStatement FromTemplate( string templateName, ITemplateProvider templateProvider, object? args = null )
        => SyntaxBuilder.CurrentImplementation.CreateTemplateStatement( new TemplateInvocation( templateName, templateProvider ), args );

    /// <summary>
    /// Creates an <see cref="IStatement"/> obtained by invoking a template specified by its name, a <see cref="TemplateProvider"/>, and optional arguments.
    /// </summary>
    public static IStatement FromTemplate( string templateName, TemplateProvider templateProvider, object? args = null )
        => SyntaxBuilder.CurrentImplementation.CreateTemplateStatement( new TemplateInvocation( templateName, templateProvider ), args );

    /// <summary>
    /// Creates a block statement (enclosed in braces) containing zero, one, or many statements.
    /// </summary>
    /// <param name="statements">The statements to include in the block.</param>
    /// <returns>An <see cref="IStatement"/> representing a block with the specified statements.</returns>
    /// <remarks>
    /// Blocks are useful for grouping multiple statements together, such as in control flow structures or when a single
    /// statement is expected but you need to execute multiple operations. The generated code will include the opening and
    /// closing braces.
    /// </remarks>
    public static IStatement Block( params IStatement[] statements ) => SyntaxBuilder.CurrentImplementation.CreateBlock( List( statements ) );

    /// <summary>
    /// Creates a block composed of zero, one or many statements.
    /// </summary>
    public static IStatement Block( IEnumerable<IStatement> statements ) => SyntaxBuilder.CurrentImplementation.CreateBlock( List( statements ) );

    /// <summary>
    /// Creates a block from an <see cref="IStatementList"/>.
    /// </summary>
    public static IStatement Block( IStatementList list ) => SyntaxBuilder.CurrentImplementation.CreateBlock( list );

    /// <summary>
    /// Extracts an <see cref="IStatementList"/> from a block statement, or creates a singleton list if the statement is not a block.
    /// </summary>
    /// <param name="statement">The statement to unwrap.</param>
    /// <returns>
    /// An <see cref="IStatementList"/> containing the statements from the block if <paramref name="statement"/> is a block,
    /// or a singleton list containing the statement itself if it is not a block.
    /// </returns>
    /// <remarks>
    /// This method is useful when you need to work with the individual statements inside a block, or when you want to treat
    /// any statement uniformly as a list of statements.
    /// </remarks>
    public static IStatementList UnwrapBlock( IStatement statement ) => SyntaxBuilder.CurrentImplementation.UnwrapBlock( statement );

    /// <summary>
    /// Creates an <see cref="IStatementList"/> from a collection of statements.
    /// </summary>
    /// <param name="statements">The statements to include in the list.</param>
    /// <returns>An <see cref="IStatementList"/> containing the specified statements.</returns>
    /// <remarks>
    /// Statement lists are used with methods like <see cref="Block(IStatementList)"/> or in switch case sections. Unlike
    /// blocks, statement lists don't add braces around the statements. Use <see cref="StatementListBuilder"/> when you need
    /// to build the list dynamically at compile time.
    /// </remarks>
    /// <seealso cref="StatementListBuilder"/>
    public static IStatementList List( params IStatement[] statements )
        => SyntaxBuilder.CurrentImplementation.CreateStatementList( statements.ToImmutableArray<object>() );

    /// <summary>
    /// Creates an <see cref="IStatementList"/> from a list of <see cref="IStatement"/>.
    /// </summary>
    public static IStatementList List( IEnumerable<IStatement> statements )
        => SyntaxBuilder.CurrentImplementation.CreateStatementList( statements.ToImmutableArray<object>() );
}