// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using static Metalama.Framework.Engine.CompileTime.TemplatingScope;

namespace Metalama.Framework.Engine.CompileTime
{
    internal static class TemplatingScopeExtensions
    {
        public static bool MustBeTransformed( this TemplatingScope scope )
            => scope != RunTimeTemplateParameter && scope.GetExpressionExecutionScope() is RunTimeOnly;

        public static bool IsCompileTimeMemberReturningRunTimeValue( this TemplatingScope scope )
            => scope is CompileTimeOnlyReturningRuntimeOnly or Dynamic or RunTimeTemplateParameter;

        public static bool EvaluatesToRunTimeValue( this TemplatingScope scope ) => scope is Dynamic or CompileTimeOnlyReturningRuntimeOnly or RunTimeOnly;

        public static bool MustExecuteAtCompileTime( this TemplatingScope scope )
            => scope is CompileTimeOnly or CompileTimeOnlyReturningBoth or CompileTimeOnlyReturningRuntimeOnly
                or Dynamic or TypeOfRunTimeType or TypeOfTemplateTypeParameter;

        public static bool CanExecuteAtCompileTime( this TemplatingScope scope ) => scope.MustExecuteAtCompileTime() || scope == RunTimeOrCompileTime;

        public static TemplatingScope ReplaceIndeterminate( this TemplatingScope scope, TemplatingScope defaultScope )
            => IsUndetermined( scope ) ? defaultScope : scope;

        public static bool IsUndetermined( this TemplatingScope scope ) => scope is RunTimeOrCompileTime or LateBound or MustFollowParent;

        public static TemplatingScope GetExpressionExecutionScope( this TemplatingScope scope, bool preferCompileTime = false )
            => scope switch
            {
                CompileTimeOnlyReturningBoth => CompileTimeOnly,
                CompileTimeOnlyReturningRuntimeOnly => CompileTimeOnly,
                Dynamic => RunTimeOnly,
                DynamicTypeConstruction => RunTimeOnly,
                RunTimeTemplateParameter => RunTimeOnly,
                TypeOfRunTimeType => RunTimeOrCompileTime,
                TypeOfTemplateTypeParameter => RunTimeOnly,
                RunTimeOrCompileTime when preferCompileTime => CompileTimeOnly,
                _ => scope
            };

        public static TemplatingScope GetExpressionValueScope( this TemplatingScope scope, bool preferCompileTime = false )
            => scope switch
            {
                CompileTimeOnlyReturningBoth when preferCompileTime => CompileTimeOnly,
                CompileTimeOnlyReturningBoth when !preferCompileTime => RunTimeOrCompileTime,
                Dynamic => RunTimeOnly,
                DynamicTypeConstruction => RunTimeOnly,
                CompileTimeOnlyReturningRuntimeOnly => RunTimeOnly,
                RunTimeTemplateParameter => RunTimeOnly,
                TypeOfRunTimeType => RunTimeOrCompileTime,
                TypeOfTemplateTypeParameter => RunTimeOnly,
                _ => scope
            };

        public static string ToDisplayString( this TemplatingScope scope )
            => scope switch
            {
                RunTimeOnly => "run-time",
                CompileTimeOnly => "compile-time",
                CompileTimeOnlyReturningRuntimeOnly => "compile-time-returning-run-time",
                CompileTimeOnlyReturningBoth => "compile-time",
                RunTimeOrCompileTime => "run-time-or-compile-time",
                TypeOfRunTimeType => "run-time-or-compile-time",
                TypeOfTemplateTypeParameter => "run-time",
                LateBound => "unbound",
                Dynamic => "run-time",
                DynamicTypeConstruction => "run-time",

                _ => scope.ToString()
            };

        public static TemplatingScope GetCombinedExecutionScope( this TemplatingScope a, TemplatingScope b ) => a.GetCombinedScope( b, true );

        public static TemplatingScope GetCombinedValueScope( this TemplatingScope a, TemplatingScope b )
        {
            return a.GetCombinedScope( b, false );
        }

        private static TemplatingScope GetCombinedScope( this TemplatingScope a, TemplatingScope b, bool isExecutionScope )
            => (a, b) switch
            {
                (RunTimeOrCompileTime, RunTimeOrCompileTime) => RunTimeOrCompileTime,
                (CompileTimeOnly, CompileTimeOnly) => CompileTimeOnly,
                (CompileTimeOnly, RunTimeOrCompileTime) => CompileTimeOnly,

                // Propagate conflicts.
                (Conflict, _) => Conflict,
                (_, Conflict) => Conflict,

                // If any part of an expression is late bound, the whole expression is also.
                (_, LateBound) => LateBound,
                (LateBound, _) => LateBound,

                (RunTimeOrCompileTime, CompileTimeOnly) => CompileTimeOnly,
                (RunTimeOrCompileTime, RunTimeOnly) => RunTimeOnly,
                (RunTimeOnly, RunTimeOrCompileTime) => RunTimeOnly,
                (RunTimeOnly, RunTimeOnly) => RunTimeOnly,
                (RunTimeOnly, CompileTimeOnly) when isExecutionScope => RunTimeOnly,
                (RunTimeOnly, CompileTimeOnly) => Conflict,
                (CompileTimeOnly, RunTimeOnly) when isExecutionScope => RunTimeOnly,
                (CompileTimeOnly, RunTimeOnly) => Conflict,

                _ => throw new AssertionFailedException( $"Invalid combination: {a}, {b}." )
            };

        public static ExecutionScope ToExecutionScope( this TemplatingScope templatingScope )
            => templatingScope.GetExpressionExecutionScope() switch
            {
                CompileTimeOnly => ExecutionScope.CompileTime,
                RunTimeOnly => ExecutionScope.RunTime,
                RunTimeOrCompileTime => ExecutionScope.RunTimeOrCompileTime,
                Conflict => ExecutionScope.RunTime, // It seems this may happen at design-time during a background rebuild.
                _ => throw new AssertionFailedException( $"Unexpected scope: {templatingScope}" )
            };
    }
}