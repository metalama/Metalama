// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;

[CompileTime]
internal sealed class ExplicitCommandNamingConvention : ICommandNamingConvention
{
    private readonly string? _commandPropertyName;
    private readonly string? _canExecuteMethodName;
    private readonly string? _canExecutePropertyName;

    public ExplicitCommandNamingConvention( string? commandPropertyName, string? canExecuteMethodName, string? canExecutePropertyName )
    {
        this._commandPropertyName = commandPropertyName;
        this._canExecuteMethodName = canExecuteMethodName;
        this._canExecutePropertyName = canExecutePropertyName;
    }

    public string Name => "explicitly-configured";

    public CommandNamingConventionMatch Match( IMethod executeMethod )
    {
        var commandName = DefaultCommandNamingConvention.GetCommandNameFromExecuteMethodName( executeMethod.Name );
        var commandPropertyName = this._commandPropertyName ?? DefaultCommandNamingConvention.GetCommandPropertyNameFromCommandName( commandName );

        return CommandNamingConventionMatcher.Match(
            this,
            executeMethod,
            commandPropertyName,
            new StringNameMatchPredicate(
                this._canExecuteMethodName != null
                    ? ImmutableArray.Create( this._canExecuteMethodName )
                    : this._canExecutePropertyName != null
                        ? ImmutableArray.Create( this._canExecutePropertyName )
                        : DefaultCommandNamingConvention.GetCanExecuteNameFromCommandName( commandName ) ),
            considerMethod: this._canExecuteMethodName != null || this._canExecutePropertyName == null,
            considerProperty: this._canExecutePropertyName != null || this._canExecuteMethodName == null,
            requireCanExecuteMatch: this._canExecuteMethodName != null || this._canExecutePropertyName != null );
    }
}