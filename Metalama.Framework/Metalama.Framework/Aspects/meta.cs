// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#pragma warning disable VSTHRD200

// ReSharper disable UnusedParameter.Global
// ReSharper disable once InconsistentNaming
namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// The entry point for the meta model, which can be used in templates to inspect the target code or access other
    /// features of the template language.
    /// </summary>
    /// <seealso href="@templates"/>
    [CompileTime]
    [TemplateKeyword]
#pragma warning disable SA1300, IDE1006 // Element should begin with upper-case letter
    public static class meta
#pragma warning restore SA1300, IDE1006 // Element should begin with upper-case letter
    {
        private static IMetaApi CurrentContext => MetalamaExecutionContext.CurrentInternal.MetaApi ?? throw CreateException();

        private static void CheckContext()
        {
            _ = CurrentContext;
        }

        private static InvalidOperationException CreateException() => new( "The 'meta' API can be used only in the execution context of a template." );

        /// <summary>
        /// Gets access to the declaration being overridden or introduced.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static IMetaTarget Target => CurrentContext.Target;

        // ReSharper disable once ReturnTypeCanBeNotNullable
        /// <summary>
        /// Invokes the logic that has been overwritten. For instance, in an <see cref="OverrideMethodAspect"/>,
        /// calling <see cref="Proceed"/> invokes the method being overridden. Note that the way how the
        /// logic is invoked (as a method call or inlining) is considered an implementation detail.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic? Proceed() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>Task&lt;dynamic?&gt;</c>.
        /// Only use this method when the return type of the method or accessor is task-like. Note that
        /// the actual return type of the overridden method or accessor is the one of the overwritten semantic, so it
        /// can be a void <see cref="Task"/>, a <see cref="ValueType"/>, or any other type.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static Task<dynamic?> ProceedAsync() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IEnumerable&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static IEnumerable<dynamic?> ProceedEnumerable() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IEnumerator&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static IEnumerator<dynamic?> ProceedEnumerator() => throw CreateException();

#if NET5_0_OR_GREATER
        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IAsyncEnumerable&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static IAsyncEnumerable<dynamic?> ProceedAsyncEnumerable() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IAsyncEnumerator&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static IAsyncEnumerator<dynamic?> ProceedAsyncEnumerator() => throw CreateException();
#endif

        /// <summary>
        /// Requests the debugger to break, if any debugger is attached to the current process.
        /// </summary>
        /// <seealso href="@debugging-aspects"/>
        [TemplateKeyword]
        [ExcludeFromCodeCoverage]
        [CompileTime( isTemplateOnly: true )]
        public static void DebugBreak() => CurrentContext.DebugBreak();

        /// <summary>
        /// Coerces an <paramref name="expression"/> to be interpreted as compile time. This is typically used
        /// to coerce expressions that can be either run-time or compile-time, such as a literal. Since ambiguous expressions are
        /// interpreted as run-time by default, this method allows to change that behavior.
        /// </summary>
        /// <param name="expression">An expression.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Exactly <paramref name="expression"/>, but coerced as a compile-time expression.</returns>
        /// <seealso href="@templates"/>
        [return: NotNullIfNotNull( "expression" )]
        [return: CompileTime]
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static T? CompileTime<T>( T? expression )
        {
            CheckContext();

            return expression;
        }

        /// <summary>
        /// Converts a compile-value into run-time value by serializing the compile-time value into a some syntax that will
        /// evaluate, at run time, to the same value as at compile time.
        /// </summary>
        /// <param name="value">A compile-time value.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A value that is structurally equivalent to the compile-time <paramref name="value"/>.</returns>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [return: NotNullIfNotNull( "value" )]
        [CompileTime( isTemplateOnly: true )]
        public static T? RunTime<T>( T? value )
        {
            CheckContext();

            return value;
        }

        /// <summary>
        /// Gets a <c>dynamic</c> object that represents an instance of the target type. It can be used as a value (e.g. as a method argument)
        /// or can be used to get access to <i>instance</i> members of the instance (e.g. <c>meta.This.MyMethod()</c>).
        /// The <see cref="This"/> property exposes the state of the target type as it is <i>after</i> the application
        /// of all aspects. If the member is <c>virtual</c>, a virtual call is performed, therefore the implementation on the child type
        /// (possibly with all applied aspects) is performed.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Final"/>.  To access the prior layer (or the base type, if there is no prior layer), use <see cref="Base"/>.
        /// To access static members, use <see cref="ThisType"/>.
        /// </summary>
        /// <seealso cref="Base"/>
        /// <seealso cref="ThisType"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic This => CurrentContext.This;

        /// <summary>
        /// Gets a <c>dynamic</c> object that must be used to get access to <i>instance</i> members of the instance (e.g. <c>meta.Base.MyMethod()</c>).
        /// The <see cref="Base"/> property exposes the state of the target type as it is <i>before</i> the application
        /// of the current aspect layer. It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Default"/>. To access the final layer, use <see cref="This"/>.
        /// To access static members, use <see cref="BaseType"/>.
        /// </summary>
        /// <seealso cref="This"/>
        /// <seealso cref="BaseType"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic Base => CurrentContext.Base;

        /// <summary>
        /// Gets a <c>dynamic</c> object that must be used to get access to <i>static</i> members of the type (e.g. <c>meta.ThisStatic.MyStaticMethod()</c>).
        /// The <see cref="ThisType"/> property exposes the state of the target type as it is <i>after</i> the application
        /// of all aspects.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Final"/>. To access the prior layer (or the base type, if there is no prior layer), use <see cref="BaseType"/>.
        /// To access instance members, use <see cref="This"/>.
        /// </summary>
        /// <seealso cref="This"/>
        /// <seealso cref="BaseType"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic ThisType => CurrentContext.ThisType;

        /// <summary>
        /// Gets a <c>dynamic</c> object that must be used to get access to <i>static</i> members of the type (e.g. <c>meta.BaseStatic.MyStaticMethod()</c>).
        /// The <see cref="BaseType"/> property exposes the state of the target type as it is <i>before</i> the application
        /// of the current aspect layer.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Default"/>. To access the final layer, use <see cref="ThisType"/>.
        /// To access instance members, use <see cref="Base"/>.
        /// </summary>
        /// <seealso cref="Base"/>
        /// <seealso cref="ThisType"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic BaseType => CurrentContext.BaseType;

        /// <summary>
        /// Gets the dictionary of tags that were passed to the <see cref="IAdviceFactory"/> method by the <see cref="IAspect{T}.BuildAspect"/> method.
        /// </summary>
        /// <seealso href="sharing-state-with-advice"/>
        public static IObjectReader Tags => CurrentContext.Tags;

        /// <summary>
        /// Gets the current <see cref="IAspectInstance"/>, which gives access to the <see cref="IAspectPredecessor.Predecessors"/>
        /// and the <see cref="IAspectInstance.SecondaryInstances"/> of the current aspect.
        /// </summary>
        /// <seealso href="@templates"/>
        public static IAspectInstance AspectInstance => CurrentContext.AspectInstance;

        /// <summary>
        /// Generates the cast syntax for the specified type.  
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value">Must be explicitly cast to <c>object</c> otherwise the C# compiler will emit an error.</param>
        /// <returns></returns>
        /// <seealso href="@templates"/>
        /// <seealso cref="ExpressionFactory.CastTo(Metalama.Framework.Code.IExpression,Metalama.Framework.Code.IType)"/>
        [TemplateKeyword]
        [return: NotNullIfNotNull( nameof(value) )]
        [CompileTime( isTemplateOnly: true )]
        public static dynamic? Cast( IType type, dynamic? value ) => ExpressionFactory.Capture( (object?) value ).CastTo( type ).Value;

        /// <summary>
        /// Generates the <c>default(T)</c> syntax for the specified type.
        /// </summary>
        /// <see cref="ExpressionFactory.Default(Metalama.Framework.Code.IType)"/>
        public static dynamic? Default( IType type ) => ExpressionFactory.Default( type ).Value;

        /// <summary>
        /// Generates the <c>default(T)</c> syntax for the specified type.
        /// </summary>
        /// <see cref="ExpressionFactory.Default(Metalama.Framework.Code.IType)"/>
        public static dynamic? Default( SpecialType type ) => ExpressionFactory.Default( type ).Value;

        /// <summary>
        /// Generates the <c>default(T)</c> syntax for the specified type.
        /// </summary>
        /// <see cref="ExpressionFactory.Default(Metalama.Framework.Code.IType)"/>
        public static dynamic? Default( Type type ) => ExpressionFactory.Default( type ).Value;

        /// <summary>
        /// Injects a comment to the target code.
        /// </summary>
        /// <param name="lines">A list of comment lines, without the <c>//</c> prefix. Null strings are processed as blank ones and will inject a blank comment line.</param>
        /// <remarks>
        /// This method is not able to add a comment to an empty block. The block must contain at least one statement.
        /// </remarks>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InsertComment( params string?[] lines ) => throw CreateException();

        /// <summary>
        /// Inserts a statement into the target code, where the statement is given as an <see cref="IStatement"/>.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InsertStatement( IStatement statement ) => throw CreateException();

        /// <summary>
        /// Inserts a statement into the target code, where the statement is given as an <see cref="IExpression"/>.
        /// Note that not all expressions can be used as statements.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InsertStatement( IExpression statement ) => throw CreateException();

        /// <summary>
        /// Inserts a statement into the target code, where the statement is given as a <see cref="string"/>.
        /// Calling this overload is equivalent to calling the <see cref="InsertStatement(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/> overload
        /// with the result of the <see cref="StatementFactory.Parse"/> method.
        /// </summary>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InsertStatement( string statement ) => throw CreateException();

        /// <summary>
        /// Calls another template method. This overload accepts a <see cref="TemplateProvider"/>.
        /// </summary>
        /// <param name="templateName">The name of the called template method.</param>
        /// <param name="templateProvider">A <see cref="TemplateProvider"/>.</param>
        /// <param name="args">Compile-time template arguments that will be passed to the template.</param>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InvokeTemplate( string templateName, TemplateProvider templateProvider, object? args = null ) => throw CreateException();

        /// <summary>
        /// Calls another template method. This overload accepts an <see cref="ITemplateProvider"/>. 
        /// </summary>
        /// <param name="templateName">The name of the called template method.</param>
        /// <param name="templateProvider">An optional <see cref="TemplateProvider"/>, or <see langword="default"/> for the current template provider (usually the current aspect).</param>
        /// <param name="args">Compile-time template arguments that will be passed to the template.</param>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InvokeTemplate( string templateName, ITemplateProvider? templateProvider = null, object? args = null ) => throw CreateException();

        /// <summary>
        /// Calls another template method.
        /// </summary>
        /// <param name="templateInvocation">Object that contains information about the called template method.</param>
        /// <param name="args">Compile-time template arguments that will be passed to the template, in addition to arguments from <paramref name="templateInvocation"/>.</param>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void InvokeTemplate( TemplateInvocation templateInvocation, object? args = null ) => throw CreateException();

        /// <summary>
        /// Inserts a <c>return;</c> statement into the target code.
        /// </summary>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static void Return() => throw CreateException();

        /// <summary>
        /// Inserts a <c>return</c> statement into the target code.
        /// This can be used to return a value from <see langword="void" />-returning template methods.
        /// </summary>
        /// <param name="value">The value to return.</param>
        [TemplateKeyword]
        public static void Return( dynamic? value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type, IExpression? value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type, dynamic value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type, IExpression? value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type, dynamic value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IExpression value ) => throw CreateException();

        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, dynamic value ) => throw CreateException();
    }
}