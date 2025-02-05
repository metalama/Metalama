// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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