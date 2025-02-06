// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// A specialization of <see cref="LogEventMetadata"/> that specifies the type of the 
/// expression model type, i.e. the type exposed to transaction policy expressions.
/// </summary>
/// <typeparam name="TExpressionModel">The type of the 
/// expression model type, i.e. the type exposed to transaction policy expressions.</typeparam>
[PublicAPI]
public abstract class LogEventMetadata<TExpressionModel> : LogEventMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEventMetadata{TExpressionModel}"/> class.
    /// </summary>
    /// <param name="name">The name of the current <see cref="LogEventMetadata"/>. This property may be undefined. It must be defined 
    /// when the <see cref="LogEventData"/> must be available for evaluation from transaction policy expressions. In this case,
    /// the type of expression model (i.e. the generic parameter of <see cref="LogEventMetadata{T}"/>) must be identical for identical
    /// values of the <see cref="LogEventMetadata.Name"/> property.</param>
    protected LogEventMetadata( string? name ) : base( name ) { }

    /// <summary>
    /// Gets the object that must be exposed to the expressions in transaction policies.
    /// </summary>
    /// <param name="data">The raw CLR object, typically <see cref="LogEventData.Data" qualifyHint="true"/>.</param>
    /// <returns></returns>
    public virtual TExpressionModel? GetExpressionModel( object? data ) => (TExpressionModel?) data;

    internal sealed override Type ExpressionModelType => typeof(TExpressionModel);
}