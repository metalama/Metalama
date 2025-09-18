// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Represents a set of instance-independent delegates used by an <see cref="ActionEventBroker{TDelegate, TArgs}"/>.
    /// </summary>
    public sealed class ActionEventBrokerDelegateSet<TDelegate, TArgs>
        where TDelegate : Delegate
    {
        public Action<TDelegate, object, TArgs> Invoker { get; }
        public Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> ToDelegate { get; }
        public Action<TDelegate, object> BaseAdd { get; }
        public Action<TDelegate, object> BaseRemove { get; }

        public ActionEventBrokerDelegateSet(
            Action<TDelegate, object, TArgs> invoker,
            Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> toDelegate,
            Action<TDelegate, object> baseAdd,
            Action<TDelegate, object> baseRemove)
        {
            this.Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.ToDelegate = toDelegate ?? throw new ArgumentNullException(nameof(toDelegate));
            this.BaseAdd = baseAdd ?? throw new ArgumentNullException(nameof(baseAdd));
            this.BaseRemove = baseRemove ?? throw new ArgumentNullException(nameof(baseRemove));
        }
    }
}