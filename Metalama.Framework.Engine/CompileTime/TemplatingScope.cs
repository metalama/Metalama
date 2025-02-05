// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Defines where a symbol or an expression can be used, i.e. in compile-time code, in run-time code, or both.
    /// </summary>
    internal enum TemplatingScope
    {
        /// <summary>
        /// The symbol can be used both at compile time or at run time.
        /// The node has not been classified as necessarily compile-time or run-time.
        /// This is typically the case for symbols of system libraries and
        /// aspects, or any declaration marked with <see cref="RunTimeOrCompileTimeAttribute"/>.
        /// </summary>
        RunTimeOrCompileTime,

        /// <summary>
        /// The symbol can be only used at run time only. This is the case for any symbol that is
        /// not contained in a system library and that is not annotated with <see cref="CompileTimeAttribute"/> or <see cref="RunTimeOrCompileTimeAttribute"/>.
        /// The node must be evaluated at run-time, but its children can be compile-time expressions.
        /// </summary>
        RunTimeOnly,

        /// <summary>
        /// The symbol can be used only at compile time. This is the case for the compile-time API of
        /// Metalama, which is marked by <see cref="CompileTimeAttribute"/>.
        /// The node including all children nodes must be evaluated at compile time.
        /// </summary>
        CompileTimeOnly,

        /// <summary>
        /// Unbound scope, for instance the scope of a lambda parameter that is not bound to a context.
        /// </summary>
        LateBound,

        /// <summary>
        /// A <see cref="CompileTimeOnly"/> member whose evaluated value is <see cref="RunTimeOnly"/>. 
        /// </summary>
        CompileTimeOnlyReturningRuntimeOnly,

        /// <summary>
        /// A <see cref="CompileTimeOnly"/> member whose evaluated value is <see cref="RunTimeOrCompileTime"/>. 
        /// </summary>
        CompileTimeOnlyReturningBoth,

        /// <summary>
        /// A member of a dynamic receiver.
        /// </summary>
        Dynamic,

        /// <summary>
        /// A type construction that includes the <c>dynamic</c> type, such as <c>dynamic[]</c>.
        /// </summary>
        DynamicTypeConstruction,

        /// <summary>
        /// An expression that contains conflicting children.
        /// </summary>
        Conflict,

        /// <summary>
        /// A run-time template parameter, generic or normal.
        /// </summary>
        RunTimeTemplateParameter,

        /// <summary>
        /// A <c>typeof(T)</c> where T is a run-time-only type, but does not reference a template argument.
        /// </summary>
        TypeOfRunTimeType,

        /// <summary>
        /// A <c>typeof(T)</c> where T is (or references) a run-time generic template parameter. 
        /// </summary>
        TypeOfTemplateTypeParameter,

        /// <summary>
        /// Used only with <see cref="SyntaxAnnotationExtensions.AddTargetScopeAnnotation{T}"/>. Means code rewriter must follow the
        /// parent and cannot rely regardless of the scope of the current node. 
        /// </summary>
        MustFollowParent,

        /// <summary>
        /// A generic type that is forced to be run-time-or-compile-time, even if its type arguments are not run-time-or-compile-time.
        /// An internal concept of <see cref="SymbolClassifier"/>, not exposed to external code.
        /// </summary>
        ForcedRunTimeOrCompileTime,

        /// <summary>
        /// A type, like System.Object, that is RunTimeOrCompileTime, but that does not cause derived types to be RunTimeOrCompileTime.
        /// An internal concept of <see cref="SymbolClassifier"/>, not exposed to external code.
        /// </summary>
        ImplicitlyRunTimeOrCompileTime
    }
}