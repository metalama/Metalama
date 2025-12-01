// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// The entry point for the T# template language, providing compile-time APIs to inspect and transform target code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>meta</c> class is a pseudo-keyword that serves as the gateway to Metalama's meta-programming capabilities.
    /// It can only be used within the context of a T# template method (methods marked with <see cref="TemplateAttribute"/>).
    /// </para>
    /// <para>
    /// Key capabilities provided by <c>meta</c>:
    /// <list type="bullet">
    /// <item><description><b>Target access:</b> <see cref="Target"/> provides metadata about the declaration being overridden or introduced</description></item>
    /// <item><description><b>Execution flow:</b> <see cref="Proceed"/> invokes the original implementation in override scenarios</description></item>
    /// <item><description><b>Dynamic typing:</b> <see cref="This"/>, <see cref="Base"/>, <see cref="ThisType"/>, <see cref="BaseType"/> for accessing members dynamically</description></item>
    /// <item><description><b>State sharing:</b> <see cref="Tags"/> to access data passed from <see cref="IAspect{T}.BuildAspect"/></description></item>
    /// <item><description><b>Code generation:</b> <see cref="InsertStatement(IStatement)"/>, <see cref="InsertComment(string?[])"/>, <see cref="Return()"/> for injecting code</description></item>
    /// <item><description><b>Type coercion:</b> <see cref="CompileTime{T}"/> and <see cref="RunTime{T}"/> to control compile-time/run-time interpretation</description></item>
    /// <item><description><b>Template invocation:</b> <see cref="InvokeTemplate(string, ITemplateProvider?, object?)"/> for calling auxiliary templates</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// In T# templates, expressions beginning with <c>meta.</c> are evaluated at compile-time to generate run-time code.
    /// The T# compiler separates compile-time portions from run-time portions using inference rules, with <c>meta</c>
    /// serving as the marker for compile-time expressions.
    /// </para>
    /// </remarks>
    /// <seealso cref="IMetaTarget"/>
    /// <seealso cref="TemplateAttribute"/>
    /// <seealso href="@templates"/>
    /// <seealso href="@template-overview"/>
    /// <seealso href="@template-compile-time"/>
    /// <seealso href="@dynamic-typing"/>
    /// <seealso href="@auxiliary-templates"/>
    [CompileTime]
    [TemplateKeyword]
#pragma warning disable SA1300, IDE1006, CS8981 // Element should begin with upper-case letter
    public static class meta
#pragma warning restore SA1300, IDE1006, CS8981 // Element should begin with upper-case letter
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
        /// <remarks>
        /// <para>
        /// The <see cref="Target"/> property provides access to metadata and invokers for the target declaration:
        /// <list type="bullet">
        /// <item><see cref="IMetaTarget.Method"/> - Access method metadata (name, parameters, return type, attributes)</item>
        /// <item><see cref="IMetaTarget.FieldOrProperty"/> - Access field/property metadata and use <c>.Value</c> to get/set the underlying value</item>
        /// <item><see cref="IMetaTarget.Event"/> - Access event metadata and use <c>.Add</c>/<c>.Remove</c> to manage handlers</item>
        /// <item><see cref="IMetaTarget.Parameters"/> - Access parameter metadata (<c>meta.Target.Parameters[0]</c>) and values (<c>meta.Target.Parameters[0].Value</c>)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <seealso cref="IMetaTarget"/>
        /// <seealso href="@overriding-methods"/>
        /// <seealso href="@overriding-fields-or-properties"/>
        /// <seealso href="@overriding-events"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static IMetaTarget Target => CurrentContext.Target;

        // ReSharper disable once ReturnTypeCanBeNotNullable
        /// <summary>
        /// Invokes the logic that has been overwritten. For instance, in an <see cref="OverrideMethodAspect"/>,
        /// calling <see cref="Proceed"/> invokes the method being overridden. Note that the way how the
        /// logic is invoked (as a method call or inlining) is considered an implementation detail.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For async and iterator methods, <see cref="Proceed"/> automatically adapts its behavior:
        /// async methods are awaited, and iterators are buffered into a collection. To avoid buffering
        /// or to use <c>await</c>/<c>yield</c> in your template, use the specialized Proceed methods
        /// (<see cref="ProceedAsync"/>, <see cref="ProceedEnumerable"/>, etc.) in conjunction with
        /// specialized template methods in <see cref="OverrideMethodAspect"/> or <see cref="MethodTemplateSelector"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="ProceedAsync"/>
        /// <seealso cref="ProceedEnumerable"/>
        /// <seealso cref="ProceedEnumerator"/>
        /// <seealso href="@overriding-methods"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic? Proceed() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>Task&lt;dynamic?&gt;</c>.
        /// Only use this method when the return type of the method or accessor is task-like. Note that
        /// the actual return type of the overridden method or accessor is the one of the overwritten semantic, so it
        /// can be a void <see cref="Task"/>, a <see cref="ValueType"/>, or any other type.
        /// </summary>
        /// <remarks>
        /// Use this method in specialized async templates (e.g., <see cref="OverrideMethodAspect.OverrideAsyncMethod"/>)
        /// to enable <c>await</c> usage in your template code and avoid automatic awaiting by the default template.
        /// </remarks>
        /// <seealso cref="Proceed"/>
        /// <seealso cref="OverrideMethodAspect.OverrideAsyncMethod"/>
        /// <seealso href="@overriding-methods"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static Task<dynamic?> ProceedAsync() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IEnumerable&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <remarks>
        /// Use this method in specialized iterator templates (e.g., <see cref="OverrideMethodAspect.OverrideEnumerableMethod"/>
        /// or <see cref="OverrideFieldOrPropertyAspect.OverrideEnumerableProperty"/>) to enable <c>yield</c> usage in your
        /// template code and avoid automatic buffering by the default template.
        /// </remarks>
        /// <seealso cref="Proceed"/>
        /// <seealso cref="OverrideMethodAspect.OverrideEnumerableMethod"/>
        /// <seealso cref="OverrideFieldOrPropertyAspect.OverrideEnumerableProperty"/>
        /// <seealso href="@overriding-methods"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        [CompileTime( isTemplateOnly: true )]
        public static IEnumerable<dynamic?> ProceedEnumerable() => throw CreateException();

        /// <summary>
        /// Synonym to <see cref="Proceed"/>, but the return type is exposed as a <c>IEnumerator&lt;dynamic?&gt;</c>.
        /// </summary>
        /// <remarks>
        /// Use this method in specialized iterator templates (e.g., <see cref="OverrideMethodAspect.OverrideEnumeratorMethod"/>
        /// or <see cref="OverrideFieldOrPropertyAspect.OverrideEnumeratorProperty"/>) to enable <c>yield</c> usage in your
        /// template code and avoid automatic buffering by the default template.
        /// </remarks>
        /// <seealso cref="Proceed"/>
        /// <seealso cref="OverrideMethodAspect.OverrideEnumeratorMethod"/>
        /// <seealso cref="OverrideFieldOrPropertyAspect.OverrideEnumeratorProperty"/>
        /// <seealso href="@overriding-methods"/>
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
        /// Gets a <c>dynamic</c> object that represents the current instance (<c>this</c>) of the object being advised. It can be used as a value (e.g. as a method argument)
        /// or can be used to get access to <i>instance</i> members of the instance (e.g. <c>meta.This.MyMethod()</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="This"/> property exposes the state of the target type as it is <i>after</i> the application
        /// of all aspects. If the member is <c>virtual</c>, a virtual call is performed, therefore the implementation on the child type
        /// (possibly with all applied aspects) is performed.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Final"/>.
        /// </para>
        /// <para>
        /// To access the prior layer (or the base type, if there is no prior layer), use <see cref="Base"/>.
        /// To access static members, use <see cref="ThisType"/>.
        /// </para>
        /// <para>
        /// Because <see cref="This"/> returns a <c>dynamic</c> value, you can access any instance member through it (e.g.,
        /// <c>meta.This._logger.WriteLine()</c>), and the actual member access will be validated and resolved when the template
        /// is expanded in the context of a specific target declaration, not when the template is compiled.
        /// </para>
        /// </remarks>
        /// <seealso cref="Base"/>
        /// <seealso cref="ExpressionFactory.This()"/>
        /// <seealso cref="ThisType"/>
        /// <seealso href="@dynamic-typing"/>
        /// <seealso href="@templates"/>
        /// <seealso cref="Receiver"/>
        [TemplateKeyword]
        public static dynamic This => CurrentContext.This;

        /// <summary>
        /// Gets a <c>dynamic</c> object that represents the receiver object, i.e. <c>this</c> in non-extension instance members, or the receiver parameter
        /// in extension members or classic extension methods.
        /// </summary>
        /// <seealso cref="This"/>
        /// <seealso cref="ExpressionFactory.Receiver()"/>
        /// <seealso cref="MemberExtensions.HasReceiver"/>
        [TemplateKeyword]
        public static dynamic Receiver => CurrentContext.ReceiverExpression( Target.Declaration );

        /// <summary>
        /// Gets a <c>dynamic</c> object that gives access to the members of the base class for the current instance, equivalent to <c>base</c> in C#.
        /// The syntax to access these members is e.g. <c>meta.Base.MyMethod()</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Base"/> property exposes the state of the target type as it is <i>before</i> the application
        /// of the current aspect layer. It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Default"/>. To access the final layer, use <see cref="This"/>.
        /// To access static members, use <see cref="BaseType"/>.
        /// </para>
        /// <para>
        /// Because <see cref="Base"/> returns a <c>dynamic</c> value, any member accessed through it is validated when the template
        /// is expanded, not when the template is compiled.
        /// </para>
        /// </remarks>
        /// <seealso cref="This"/>
        /// <seealso cref="BaseType"/>
        /// <seealso href="@dynamic-typing"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic Base => CurrentContext.Base;

        /// <summary>
        /// Gets a <c>dynamic</c> object that must be used to get access to <i>static</i> members of the type (e.g. <c>meta.ThisStatic.MyStaticMethod()</c>).
        /// If the current declaration is an extension member, the property returns the declaring type, <i>not</i> the receiver type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="ThisType"/> property exposes the state of the target type as it is <i>after</i> the application
        /// of all aspects.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Final"/>. To access the prior layer (or the base type, if there is no prior layer), use <see cref="BaseType"/>.
        /// To access instance members, use <see cref="This"/>.
        /// </para>
        /// <para>
        /// Because <see cref="ThisType"/> returns a <c>dynamic</c> value, static members accessed through it are validated at
        /// template expansion time, not at template compilation time.
        /// </para>
        /// </remarks>
        /// <seealso cref="This"/>
        /// <seealso cref="BaseType"/>
        /// <seealso href="@dynamic-typing"/>
        /// <seealso href="@templates"/>
        [TemplateKeyword]
        public static dynamic ThisType => CurrentContext.ThisType;

        /// <summary>
        /// Gets a <c>dynamic</c> object that must be used to get access to <i>static</i> members of the type (e.g. <c>meta.BaseStatic.MyStaticMethod()</c>).
        /// If the current declaration is an extension member, the property returns the declaring type, <i>not</i> the receiver type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="BaseType"/> property exposes the state of the target type as it is <i>before</i> the application
        /// of the current aspect layer.  It corresponds to <see cref="InvokerOptions"/>.<see cref="InvokerOptions.Default"/>. To access the final layer, use <see cref="ThisType"/>.
        /// To access instance members, use <see cref="Base"/>.
        /// </para>
        /// <para>
        /// Because <see cref="BaseType"/> returns a <c>dynamic</c> value, static members accessed through it are validated at
        /// template expansion time, not at template compilation time.
        /// </para>
        /// </remarks>
        /// <seealso cref="Base"/>
        /// <seealso cref="ThisType"/>
        /// <seealso href="@dynamic-typing"/>
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

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified type but no initial value.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The type of the variable.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <remarks>
        /// <para>
        /// Use this method when you need to programmatically define local variables, for example, within a compile-time <c>foreach</c> loop
        /// where the number of variables is determined at compile-time. Unlike normal template variable declarations which are automatically
        /// classified as run-time or compile-time, <see cref="DefineLocalVariable(string, IType)"/> always creates a run-time variable.
        /// </para>
        /// <para>
        /// The returned <see cref="IExpression"/> can be used to read or write the variable value in subsequent template code.
        /// </para>
        /// </remarks>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified type and initial value expression.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The type of the variable.</param>
        /// <param name="value">An expression representing the initial value, or <c>null</c> for no initialization.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <remarks>
        /// <para>
        /// Use this method when you need to programmatically define local variables with compile-time determined initial values.
        /// This is useful when generating code that saves and restores values, such as rolling back field changes upon exception.
        /// </para>
        /// </remarks>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type, IExpression? value ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified type and initial value.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The type of the variable.</param>
        /// <param name="value">The initial value for the variable.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IType type, dynamic value ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified reflection type but no initial value.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The reflection type of the variable.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified reflection type and initial value expression.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The reflection type of the variable.</param>
        /// <param name="value">An expression representing the initial value, or <c>null</c> for no initialization.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type, IExpression? value ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a specified reflection type and initial value.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="type">The reflection type of the variable.</param>
        /// <param name="value">The initial value for the variable.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, Type type, dynamic value ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a type inferred from the initial value expression.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="value">An expression representing the initial value. The type is inferred from this expression.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <remarks>
        /// This overload infers the variable type from the <paramref name="value"/> expression, similar to using <c>var</c> in C#.
        /// </remarks>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, IExpression value ) => throw CreateException();

        /// <summary>
        /// Programmatically defines a local variable in run-time code with a type inferred from the initial value.
        /// </summary>
        /// <param name="nameHint">A hint for the variable name. Metalama automatically appends a numerical suffix to ensure uniqueness within the target lexical scope.</param>
        /// <param name="value">The initial value. The type is inferred from this value.</param>
        /// <returns>An <see cref="IExpression"/> that can be used to reference the local variable in run-time code.</returns>
        /// <remarks>
        /// This overload infers the variable type from the <paramref name="value"/>, similar to using <c>var</c> in C#.
        /// </remarks>
        /// <seealso href="@run-time-statements"/>
        /// <seealso href="@templates"/>
        [CompileTime( isTemplateOnly: true )]
        public static IExpression DefineLocalVariable( string nameHint, dynamic value ) => throw CreateException();
    }
}