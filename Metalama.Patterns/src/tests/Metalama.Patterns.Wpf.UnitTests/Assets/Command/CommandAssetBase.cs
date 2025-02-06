// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.Command;

public abstract class CommandAssetBase
{
    public sealed class ThreadContext
    {
        private static readonly ThreadLocal<ThreadContext> _current = new( () => new ThreadContext() );

        public static ThreadContext Current => _current.Value!;

        public void Reset( Func<int?, bool>? canExecute = null )
        {
            this.Log.Clear();
            this.CanExecute = canExecute;
        }

        public List<string> Log { get; } = new();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public Func<int?, bool>? CanExecute { get; set; }
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    public static void LogCall( string? suffix = null, [CallerMemberName] string? name = null )
    {
        ThreadContext.Current.Log.Add( suffix == null ? name! : $"{name}|{suffix}" );
    }

    protected static bool CanExecute( int? value = null )
    {
        var f = ThreadContext.Current.CanExecute;

        return f == null || f( value );
    }
}