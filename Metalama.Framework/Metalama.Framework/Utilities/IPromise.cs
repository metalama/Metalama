// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

// ReSharper disable UnusedMemberInSuper.Global

namespace Metalama.Framework.Utilities;

/// <summary>
/// Encapsulates value that must be defined later. Promises can used to pass introduced declarations to templates as arguments
/// when these declarations have not been introduced yet, resolving a chicken-or-egg situation. When objects of type <see cref="IPromise"/> are passed to a template,
/// the template will automatically receive its resolved <see cref="Value"/> instead of the <see cref="IPromise"/> object. The <see cref="Promise{T}"/> class
/// implements this interface.
/// </summary>
[CompileTime]
public interface IPromise
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="Value"/> setter has been successfully invoked. 
    /// </summary>
    bool IsResolved { get; }

    /// <summary>
    /// Gets a value indicating whether the promise is faulted. In this case, the <see cref="Exception"/> property is set.
    /// </summary>
    bool IsFaulted { get; }

    /// <summary>
    /// Gets the value. Throws <see cref="InvalidOperationException"/> if <see cref="IPromise.IsResolved"/> is <c>false</c>.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets the <see cref="System.Exception"/> that the promise resulted in, if <see cref="IsFaulted"/> is <c>true</c>.
    /// </summary>
    Exception? Exception { get; }
}

/// <summary>
/// Encapsulates value that must be defined later. Promises can used to pass introduced declarations to templates as arguments
/// when these declarations have not been introduced yet, resolving a chicken-or-egg situation. When objects of type <see cref="IPromise"/> are passed to a template,
/// the template will automatically receive its resolved <see cref="Value"/> instead of the <see cref="IPromise"/> object. The <see cref="Promise{T}"/> class
/// implements this interface.
/// </summary>
public interface IPromise<out T> : IPromise
{
    /// <summary>
    /// Gets the value. Throws <see cref="InvalidOperationException"/> if <see cref="IPromise.IsResolved"/> is <c>false</c>.
    /// </summary>
    new T Value { get; }
}