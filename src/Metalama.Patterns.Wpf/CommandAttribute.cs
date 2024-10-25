// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    void IEligible<IMethod>.BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        builder.ReturnType().MustEqual( SpecialType.Void );
        builder.MustSatisfy( m => m.Parameters.Count is 0 or 1, m => $"{m} must have zero or one parameter" );
        builder.MustNotHaveRefOrOutParameter();
        builder.MustSatisfy( m => m.TypeParameters.Count == 0, m => $"{m} must not be generic" );
    }

    void IAspect<IMethod>.BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var target = builder.Target;
        var declaringType = target.DeclaringType;
        var options = target.Enhancements().GetOptions<CommandOptions>();

        if ( this is { CanExecuteMethod: not null, CanExecuteProperty: not null } )
        {
            builder.Diagnostics.Report( Diagnostics.CannotSpecifyBothCanExecuteMethodAndCanExecuteProperty );

            // Further diagnostics would be confusing and transformation is not possible.

            return;
        }

        var hasExplicitCanExecuteNaming = this.CanExecuteMethod != null || this.CanExecuteProperty != null;

        var namingConventions = hasExplicitCanExecuteNaming
            ? [new ExplicitCommandNamingConvention( this.CommandPropertyName, this.CanExecuteMethod, this.CanExecuteProperty )]
            : options.GetSortedNamingConventions();

        var diagnosticReporter = new DiagnosticReporter( builder );

        if ( !NamingConventionEvaluator.TryEvaluate( namingConventions, target, diagnosticReporter, out var match ) )
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

        var introducePropertyResult = builder.Advice.IntroduceProperty(
            declaringType,
            nameof(CommandProperty),
            IntroductionScope.Instance,
            OverrideStrategy.Fail,
            b =>
            {
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
                        Diagnostics.CommandNotifiableCanExecutePropertyIsNotPublic.WithArguments( target ),
                        canExecuteProperty );
                }

                useInpcIntegration = true;
            }
        }

        if ( !MetalamaExecutionContext.Current.ExecutionScenario.CapturesNonObservableTransformations )
        {
            builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, target );

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
            nameof(InitializeCommand),
            InitializerKind.BeforeInstanceConstructor,
            args: new
            {
                commandProperty,
                executeMethod = target,
                canExecuteMethod,
                canExecuteProperty,
                useInpcIntegration
            } );
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [Template]
    private static ICommand CommandProperty { get; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Template]
    private static void InitializeCommand(
        [CompileTime] IProperty commandProperty,
        [CompileTime] IMethod executeMethod,
        [CompileTime] IMethod? canExecuteMethod,
        [CompileTime] IProperty? canExecuteProperty,
        [CompileTime] bool useInpcIntegration )
    {
        IExpression? canExecuteExpression = null;

        if ( canExecuteMethod != null || canExecuteProperty != null )
        {
            if ( canExecuteMethod != null )
            {
                if ( canExecuteMethod.Parameters.Count == 0 )
                {
                    canExecuteExpression = ExpressionFactory.Capture( new Func<object, bool>( _ => (bool) canExecuteMethod.Invoke()! ) );
                }
                else
                {
                    canExecuteExpression = ExpressionFactory.Capture(
                        new Func<object, bool>( parameter => (bool) canExecuteMethod.Invoke( meta.Cast( canExecuteMethod.Parameters[0].Type, parameter ) ) ) );
                }
            }
            else
            {
                canExecuteExpression = ExpressionFactory.Capture( new Func<object, bool>( _ => (bool) canExecuteProperty!.Value! ) );
            }
        }

        IExpression? executeExpression;

        if ( executeMethod.Parameters.Count == 0 )
        {
            executeExpression = ExpressionFactory.Capture( new Action<object>( _ => { executeMethod.Invoke(); } ) );
        }
        else
        {
            executeExpression = ExpressionFactory.Capture(
                new Action<object>(
                    parameter =>
                    {
                        executeMethod.Invoke( meta.Cast( executeMethod.Parameters[0].Type, parameter ) );
                    } ) );
        }

        if ( useInpcIntegration )
        {
            commandProperty.Value = new DelegateCommand( executeExpression.Value!, canExecuteExpression!.Value!, meta.This, canExecuteProperty!.Name );
        }
        else
        {
            // ReSharper disable once MergeConditionalExpression
#pragma warning disable IDE0031 // Use null propagation
            commandProperty.Value = new DelegateCommand( executeExpression.Value!, canExecuteExpression == null ? null : canExecuteExpression.Value );
#pragma warning restore IDE0031 // Use null propagation
        }
    }
}