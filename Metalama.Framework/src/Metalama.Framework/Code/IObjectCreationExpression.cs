// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// A compile-time representation of a run-time object creation expression.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    [Hidden]
    public interface IObjectCreationExpression : IExpression
    {
        public IExpression WithObjectInitializer( params (IFieldOrProperty FieldOrProperty, IExpression Value)[] initializationExpressions );

        public IExpression WithObjectInitializer( params (string FieldOrPropertyName, IExpression Value)[] initializationExpressions );

        // TODO: WithCollectionInitializer, WithDictionaryInitializer, WithComplexInitializer.
    }
}