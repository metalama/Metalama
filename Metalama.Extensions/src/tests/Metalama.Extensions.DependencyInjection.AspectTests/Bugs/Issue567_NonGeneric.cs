// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue567_NonGeneric;

public interface ILastChanceExceptionHandler
{
    void OnException( Exception e );
}

public interface IMutable;

public class Copied : IMutable;

// <target>
public sealed class ExceptionAwareAttribute : OverrideMethodAspect
{
    [IntroduceDependency( IsRequired = true )]
    private readonly ILastChanceExceptionHandler? _exceptionHandler = null;

    // ...
    public override dynamic? OverrideMethod()
    {
        try
        {
            return meta.Proceed();
        }
        catch ( Exception e )
        {
            this._exceptionHandler!.OnException( e );

            throw;
        }
    }
}

// <target>
public abstract class ModelBase
{
    [ExceptionAware]
    protected Task Initialize( CancellationToken parameter ) => Task.CompletedTask;
}

public abstract class UpdateModelBase : ModelBase
{
    private readonly IMutable _last;

    protected UpdateModelBase() : this( new Copied() ) { }

    protected UpdateModelBase( IMutable last )
    {
        this._last = last;
    }
}