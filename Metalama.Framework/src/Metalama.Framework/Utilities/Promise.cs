// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Utilities;

/// <summary>
/// Encapsulates value that must be defined later. Promises can used to to pass introduced declarations to templates as arguments
/// when these declarations have not been introduced yet, resolving a chicken-or-egg situation. When objects of type <see cref="IPromise"/> are passed to a template,
/// the template will automatically receive its resolved <see cref="Value"/> instead of the <see cref="IPromise"/> object. 
/// </summary>
[CompileTime]
[PublicAPI]
public sealed class Promise<T> : IPromise<T>
{
    private T? _value;

    /// <inheritdoc />
    public Exception? Exception { get; private set; }

    /// <inheritdoc />
    public bool IsResolved { get; private set; }

    /// <inheritdoc />
    public bool IsFaulted => this.Exception != null;

    object? IPromise.Value => this.Value;

    /// <summary>
    /// Sets the <see cref="System.Exception"/> in which the promise resulted and sets the <see cref="IsFaulted"/> property to <c>true</c>.
    /// </summary>
    public void SetException( Exception exception )
    {
        this.CheckAssignable();

        this.Exception = exception;
    }

    /// <summary>
    /// Gets or sets the deferred value. Getting the property throws an <see cref="InvalidOperationException"/> if it has not been set before.
    /// </summary>
    public T Value
    {
        get
        {
            if ( !this.IsResolved )
            {
                throw new InvalidOperationException( $"The Value of the {this.GetType()} must be set before it can be read." );
            }
            else if ( this.Exception != null )
            {
                throw this.Exception;
            }

            return this._value!;
        }

        set
        {
            this.CheckAssignable();

            this._value = value;
            this.IsResolved = true;
        }
    }

    private void CheckAssignable()
    {
        if ( this.IsResolved )
        {
            throw new InvalidOperationException( $"The Value of the {this.GetType()} has already been set." );
        }
        else if ( this.IsFaulted )
        {
            throw new InvalidOperationException( $"The {this.GetType()} has already failed." );
        }
    }
}