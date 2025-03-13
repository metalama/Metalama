// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal static class CompileTimeMocksHelper
    {
        // Coverage: ignore
        public static Exception CreateNotSupportedException( string typeName )
            => new NotSupportedException(
                $"This instance of {typeName} cannot be accessed at compile time because it represents a run-time object. Try using meta.RunTime() to convert this object to its run-time value." );

        public static TypedExpressionSyntax ToTypedExpressionSyntax<T>(
            ICompileTimeReflectionObject<T> compileTimeMemberInfo,
            Func<T, SyntaxSerializationContext, ExpressionSyntax> serialize,
            ISyntaxGenerationContext syntaxGenerationContext )
            where T : class, ICompilationElement
        {
            var serializationContext = (SyntaxSerializationContext) syntaxGenerationContext;

            return ToTypedExpressionSyntax(
                compileTimeMemberInfo.Target.GetTarget( serializationContext.CompilationModel ),
                compileTimeMemberInfo.ReflectionType,
                serialize,
                syntaxGenerationContext );
        }

        public static TypedExpressionSyntax ToTypedExpressionSyntax<T>(
            T member,
            Type type,
            Func<T, SyntaxSerializationContext, ExpressionSyntax> serialize,
            ISyntaxGenerationContext syntaxGenerationContext )
        {
            var serializationContext = (SyntaxSerializationContext) syntaxGenerationContext;

            var expression = serialize( member, serializationContext );

            var iType = serializationContext.CompilationModel.Factory.GetTypeByReflectionType( type );

            return new TypedExpressionSyntax(
                new TypedExpressionSyntaxImpl(
                    expression,
                    iType,
                    serializationContext.CompilationModel,
                    true ) );
        }
    }
}