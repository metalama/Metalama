// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Project;
using Metalama.Patterns.Wpf.Configuration;
using Metalama.Patterns.Wpf.Implementation;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.ComponentModel;
using System.Windows.Input;
using MetalamaAccessibility = Metalama.Framework.Code.Accessibility;

// TODO: Skip [Observable] on [Command]-targeted auto properties. No functional impact, would just avoid unnecessary generated code.

namespace Metalama.Patterns.Wpf;

[PublicAPI]
[AttributeUsage( AttributeTargets.Method )]
public sealed partial class CommandAttribute : Attribute, IAspect<IMethod>
{
    internal const string CommandPropertyCategory = "command property";
    internal const string CanExecuteMethodCategory = "can-execute method";
    internal const string CanExecutePropertyCategory = "can-execute property";

    /// <summary>
    /// Gets or sets the name of the <see cref="ICommand"/> property that is introduced.
    /// </summary>
    public string? CommandPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the name of the method that is called to determine whether the command can be executed.
    /// This method corresponds to the <see cref="ICommand.CanExecute"/> method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>CanExecute</c> method must be declared in the same class as the command property, return a <c>bool</c> value and can have zero or one parameter.
    /// </para>
    /// <para>
    /// If this property is not set, then the aspect will look for a method named <c>CanFoo</c> or <c>CanExecuteFoo</c>, where <c>Foo</c> is the name of the command without the <c>Command</c> suffix.
    /// </para>
    /// <para>
    /// At most one of the <see cref="CanExecuteMethod"/> and <see cref="CanExecuteProperty"/> properties may be set.
    /// </para>
    /// </remarks>
    public string? CanExecuteMethod { get; set; }

    /// <summary>
    /// Gets or sets the name of the property that is evaluated to determine whether the command can be executed.
    /// This property corresponds to the <see cref="ICommand.CanExecute"/> method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>CanExecute</c> property must be declared in the same class as the command property and return a <c>bool</c> value.
    /// </para>
    /// <para>
    /// If this property is not set, then the aspect will look for a property named <c>CanFoo</c> or <c>CanExecuteFoo</c>, where <c>Foo</c> is the name of the command without the <c>Command</c> suffix.
    /// </para>
    /// <para>
    /// At most one of the <see cref="CanExecuteMethod"/> and <see cref="CanExecuteProperty"/> properties may be set.
    /// </para>
    /// </remarks>
    public string? CanExecuteProperty { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether integration with <see cref="INotifyPropertyChanged"/> is enabled. The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="EnableINotifyPropertyChangedIntegration"/> is <see langword="true"/> (the default), and when a can-execute property (not a method) is used,
    /// and when the containing type of the target property implements <see cref="INotifyPropertyChanged"/>,then the <see cref="ICommand.CanExecuteChanged"/> event of 
    /// the command will be raised when the can-execute property changes. A warning is reported if the can-execute property is not public because <see cref="INotifyPropertyChanged"/>
    /// implementations typically only notify changes to public properties.
    /// </para>
    /// </remarks>
    public bool? EnableINotifyPropertyChangedIntegration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether several executions of the command can run concurrently. This property is only considered for asynchronous methods.
    /// Its default value is <c>false</c>, which means that the <see cref="ICommand.CanExecute"/> method will return <c>false</c> if another execution is still running.
    /// This property is ignored if the execution method is non-<see cref="Task"/> and the <see cref="Background"/> property is left to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>When <see cref="SupportsConcurrentExecution"/> is <c>true</c>, the <see cref="BaseAsyncDelegateCommand.ExecutionTask"/> is set
    /// to the last started task, and the <see cref="BaseAsyncDelegateCommand.Cancel"/> only cancels the last started task. To track or cancel individual executions,
    /// use the <see cref="DelegateCommandExecution"/> returned by the <see cref="AsyncDelegateCommand.Execute"/> method.</para>
    /// </remarks>
    public bool SupportsConcurrentExecution { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command will execute in a background thread. The default value is <c>false</c>, meaning that the command will be
    /// executed in the UI thread. If this property is set to <c>true</c>, a property of type <see cref="AsyncDelegateCommand"/> is generated,
    /// even for non-<see cref="Task"/> execution methods.
    /// </summary>
    public bool Background { get; set; }

    void IEligible<IMethod>.BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        builder.MustNotHaveRefOrOutParameter();
        builder.MustSatisfy( m => m.TypeParameters.Count == 0, m => $"{m} must not be generic" );

        builder.ReturnType().MustSatisfyAny( b => b.MustEqual( SpecialType.Void ), b => b.MustEqual( SpecialType.Task ) );

        builder.MustSatisfy( m => m.Parameters.Count <= 2, m => $"{m} must have 2 or fewer parameters" );

        builder.If( m => m.Parameters.Count == 2 )
            .MustSatisfy(
                m => m.Parameters[^1].Type.Equals( typeof(CancellationToken) ),
                m => $"if {m} has two parameters, the last one must be a CancellationToken" );
    }

    void IAspect<IMethod>.BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var executeMethod = builder.Target;
        var declaringType = executeMethod.DeclaringType;
        var options = executeMethod.Enhancements().GetOptions<CommandOptions>();

        if ( this is { CanExecuteMethod: not null, CanExecuteProperty: not null } )
        {
            builder.Diagnostics.Report( Diagnostics.CannotSpecifyBothCanExecuteMethodAndCanExecuteProperty );

            // Further diagnostics would be confusing and transformation is not possible.

            return;
        }

        var hasExplicitCanExecuteNaming = this.CanExecuteMethod != null || this.CanExecuteProperty != null;

        // Find the CanExecute method or property.
        var namingConventions = hasExplicitCanExecuteNaming
            ? [new ExplicitCommandNamingConvention( this.CommandPropertyName, this.CanExecuteMethod, this.CanExecuteProperty )]
            : options.GetSortedNamingConventions();

        var diagnosticReporter = new DiagnosticReporter( builder );

        if ( !NamingConventionEvaluator.TryEvaluate( namingConventions, executeMethod, diagnosticReporter, out var match ) )
        {
            builder.SkipAspect();

            return;
        }

        IProperty? commandProperty;
        IMethod? canExecuteMethod = null;
        IProperty? canExecuteProperty = null;

        switch ( match.CanExecuteMatch.Member )
        {
            case null:
                break;

            case IProperty property:
                canExecuteProperty = property;

                break;

            case IMethod method:
                canExecuteMethod = method;

                break;

            default:
                throw new NotSupportedException( "Expected a method or property." );
        }

        // Determines the type of command we need to plug.
        var isAsyncCommand = executeMethod.ReturnType.SpecialType == SpecialType.Task;
        var supportsCancellation = executeMethod.Parameters.Count > 0 && executeMethod.Parameters[^1].Type.Equals( typeof(CancellationToken) );

        if ( supportsCancellation && !isAsyncCommand && !this.Background )
        {
            builder.Diagnostics.Report( Diagnostics.CancellationInNonAsyncNotSupported.WithArguments( executeMethod ) );

            return;
        }

        var parameterType = executeMethod.Parameters.Count > 0 && !executeMethod.Parameters[0].Type.Equals( typeof(CancellationToken) )
            ? executeMethod.Parameters[0].Type
            : null;

        var (propertyTemplate, commandType) = (isAsyncCommand, parameterType, this.Background) switch
        {
            (false, null, false) => (nameof(CommandProperty), TypeFactory.GetType( typeof(DelegateCommand) )),
            (false, not null, false) => (nameof(CommandProperty),
                                         ((INamedType) TypeFactory.GetType( typeof(DelegateCommand<>) )).MakeGenericInstance( parameterType )),
            (true, null, _) or (false, null, true) => (nameof(AsyncCommandProperty), TypeFactory.GetType( typeof(AsyncDelegateCommand) )),
            (true, not null, _) or (false, not null, true) => (nameof(AsyncCommandProperty),
                                                               ((INamedType) TypeFactory.GetType( typeof(AsyncDelegateCommand<>) )).MakeGenericInstance(
                                                                   parameterType ))
        };

        // Introduce the Command property.
        var introducePropertyResult = builder.Advice.IntroduceProperty(
            declaringType,
            propertyTemplate,
            IntroductionScope.Instance,
            OverrideStrategy.Fail,
            b =>
            {
                b.Type = commandType;
                b.Name = match.CommandPropertyName!;

                // ReSharper disable once RedundantNameQualifier
                b.Accessibility = MetalamaAccessibility.Public;

                // ReSharper disable once RedundantNameQualifier
                b.GetMethod!.Accessibility = MetalamaAccessibility.Public;
            } );

        if ( introducePropertyResult.Outcome == AdviceOutcome.Default )
        {
            commandProperty = introducePropertyResult.Declaration;
        }
        else
        {
            builder.SkipAspect();

            return;
        }

        var useInpcIntegration = false;

        if ( canExecuteProperty != null && options.EnableINotifyPropertyChangedIntegration == true )
        {
            if ( declaringType.AllImplementedInterfaces.Contains( typeof(INotifyPropertyChanged) ) )
            {
                // ReSharper disable once RedundantNameQualifier
                if ( canExecuteProperty.Accessibility != MetalamaAccessibility.Public )
                {
                    builder.Diagnostics.Report(
                        Diagnostics.CommandNotifiableCanExecutePropertyIsNotPublic.WithArguments( executeMethod ),
                        canExecuteProperty );
                }

                useInpcIntegration = true;
            }
        }

        if ( !MetalamaExecutionContext.Current.ExecutionScenario.CapturesNonObservableTransformations )
        {
            builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, executeMethod );

            if ( canExecuteMethod != null )
            {
                builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, canExecuteMethod );
            }

            if ( canExecuteProperty != null )
            {
                builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, canExecuteProperty );
            }

            return;
        }

        builder.Advice.AddInitializer(
            declaringType,
            parameterType != null ? nameof(this.InitializeCommandWithParameter) : nameof(this.InitializeCommandWithoutParameter),
            InitializerKind.BeforeInstanceConstructor,
            args: new
            {
                commandProperty,
                executeMethod,
                canExecuteMethod,
                canExecuteProperty,
                useInpcIntegration,
                isAsyncCommand,
                T = parameterType,
                supportsCancellation
            } );
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [Template]
    private static dynamic CommandProperty { get; }

    [Template]
    private static dynamic AsyncCommandProperty { get; }

    [Template]
    private void InitializeCommandWithoutParameter(
        [CompileTime] IProperty commandProperty,
        [CompileTime] IMethod executeMethod,
        [CompileTime] IMethod? canExecuteMethod,
        [CompileTime] IProperty? canExecuteProperty,
        [CompileTime] bool useInpcIntegration,
        [CompileTime] bool isAsyncCommand,
        [CompileTime] bool supportsCancellation )
    {
        IExpression? canExecuteExpression = null;

        if ( canExecuteMethod != null || canExecuteProperty != null )
        {
            canExecuteExpression = canExecuteMethod != null
                ? ExpressionFactory.Parse( canExecuteMethod.Name )
                : ExpressionFactory.Capture( new Func<bool>( () => (bool) canExecuteProperty!.Value! ) );
        }

// ReSharper disable ConvertToLambdaExpression

        var executeExpression = ExpressionFactory.Parse( executeMethod.Name );

        if ( !isAsyncCommand )
        {
            if ( !this.Background )
            {
                if ( useInpcIntegration )
                {
                    commandProperty.Value = DelegateCommandFactory.CreateDelegateCommand(
                        executeExpression.Value!,
                        canExecuteExpression!.Value!,
                        meta.This,
                        canExecuteProperty!.Name );
                }
                else
                {
                    // ReSharper disable once MergeConditionalExpression
                    commandProperty.Value = DelegateCommandFactory.CreateDelegateCommand(
                        executeExpression.Value!,
                        canExecuteExpression == null ? null : canExecuteExpression.Value );
                }
            }
            else
            {
                if ( useInpcIntegration )
                {
                    commandProperty.Value = DelegateCommandFactory.CreateBackgroundDelegateCommand(
                        executeExpression.Value!,
                        canExecuteExpression!.Value!,
                        meta.This,
                        canExecuteProperty!.Name,
                        this.SupportsConcurrentExecution );
                }
                else
                {
                    // ReSharper disable once MergeConditionalExpression
                    commandProperty.Value = DelegateCommandFactory.CreateBackgroundDelegateCommand(
                        executeExpression.Value!,
                        canExecuteExpression == null ? null : canExecuteExpression.Value,
                        this.SupportsConcurrentExecution );
                }
            }
        }
        else
        {
            if ( useInpcIntegration )
            {
                commandProperty.Value = DelegateCommandFactory.CreateAsyncDelegateCommand(
                    executeExpression.Value!,
                    canExecuteExpression!.Value!,
                    meta.This,
                    canExecuteProperty!.Name,
                    this.SupportsConcurrentExecution,
                    this.Background );
            }
            else
            {
                // ReSharper disable once MergeConditionalExpression
                commandProperty.Value = DelegateCommandFactory.CreateAsyncDelegateCommand(
                    executeExpression.Value!,
                    canExecuteExpression == null ? null : canExecuteExpression.Value,
                    this.SupportsConcurrentExecution,
                    this.Background );
            }
        }

// ReSharper restore ConvertToLambdaExpression        
    }

    [Template]
    private void InitializeCommandWithParameter<[CompileTime] T>(
        [CompileTime] IProperty commandProperty,
        [CompileTime] IMethod executeMethod,
        [CompileTime] IMethod? canExecuteMethod,
        [CompileTime] IProperty? canExecuteProperty,
        [CompileTime] bool useInpcIntegration,
        [CompileTime] bool isAsyncCommand,
        [CompileTime] bool supportsCancellation )
    {
        IExpression? canExecuteExpression = null;

        meta.DebugBreak();

        if ( canExecuteMethod != null || canExecuteProperty != null )
        {
            canExecuteExpression = canExecuteMethod != null
                ? ExpressionFactory.Parse( canExecuteMethod.Name )
                : ExpressionFactory.Capture( new Func<T, bool>( _ => (bool) canExecuteProperty!.Value! ) );
        }

// ReSharper disable ConvertToLambdaExpression

        var executeExpression = ExpressionFactory.Parse( executeMethod.Name );

        if ( !isAsyncCommand )
        {
            if ( !this.Background )
            {
                if ( useInpcIntegration )
                {
                    commandProperty.Value = DelegateCommandFactory.CreateDelegateCommand<T>(
                        executeExpression.Value!,
                        canExecuteExpression!.Value!,
                        meta.This,
                        canExecuteProperty!.Name );
                }
                else
                {
                    // ReSharper disable once MergeConditionalExpression
                    commandProperty.Value = DelegateCommandFactory.CreateDelegateCommand<T>(
                        executeExpression.Value!,
                        canExecuteExpression == null ? null : canExecuteExpression.Value );
                }
            }
            else
            {
                if ( useInpcIntegration )
                {
                    commandProperty.Value = DelegateCommandFactory.CreateBackgroundDelegateCommand<T>(
                        executeExpression.Value!,
                        canExecuteExpression!.Value!,
                        meta.This,
                        canExecuteProperty!.Name,
                        this.SupportsConcurrentExecution );
                }
                else
                {
                    // ReSharper disable once MergeConditionalExpression
                    commandProperty.Value = DelegateCommandFactory.CreateBackgroundDelegateCommand<T>(
                        executeExpression.Value!,
                        canExecuteExpression == null ? null : canExecuteExpression.Value,
                        this.SupportsConcurrentExecution );
                }
            }
        }
        else
        {
            if ( useInpcIntegration )
            {
                commandProperty.Value = DelegateCommandFactory.CreateAsyncDelegateCommand<T>(
                    executeExpression.Value!,
                    canExecuteExpression!.Value!,
                    meta.This,
                    canExecuteProperty!.Name,
                    this.SupportsConcurrentExecution,
                    this.Background );
            }
            else
            {
                // ReSharper disable once MergeConditionalExpression
                commandProperty.Value = DelegateCommandFactory.CreateAsyncDelegateCommand<T>(
                    executeExpression.Value!,
                    canExecuteExpression == null ? null : canExecuteExpression.Value,
                    this.SupportsConcurrentExecution,
                    this.Background );
            }
        }

// ReSharper restore ConvertToLambdaExpression        
    }
}