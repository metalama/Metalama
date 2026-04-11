// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Initialization;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public class InitializationContextTests
{
    [Fact]
    public void DefaultContext_HasNoneIntent()
    {
        var context = default(InitializationContext);

        Assert.Equal( CallerIntent.None, context.Intent );
    }

    [Fact]
    public void DefaultContext_WillCallOnInitialized_IsFalse()
    {
        var context = default(InitializationContext);

        Assert.False( context.WillCallOnInitialized );
    }

    [Fact]
    public void WillInitialize_HasCorrectIntent()
    {
        Assert.Equal( CallerIntent.WillInitialize, InitializationContext.WillInitialize.Intent );
    }

    [Fact]
    public void WillCallOnInitialized_TrueForWillInitialize()
    {
        Assert.True( InitializationContext.WillInitialize.WillCallOnInitialized );
    }

    [Fact]
    public void IsHandled_ReturnsTrueForMatchingSlot()
    {
        var factory = new InitializationSlotFactory();
        var slot = factory.Allocate();
        var context = InitializationContext.WillInitialize.Descend( slot );

        Assert.True( context.IsHandled( slot ) );
    }

    [Fact]
    public void IsHandled_ReturnsFalseForUnrelatedSlot()
    {
        var factory = new InitializationSlotFactory();
        var slotA = factory.Allocate();
        var slotB = factory.Allocate();
        var context = InitializationContext.WillInitialize.Descend( slotA );

        Assert.False( context.IsHandled( slotB ) );
    }

    [Fact]
    public void Descend_CombinesSlots()
    {
        var factory = new InitializationSlotFactory();
        var slotA = factory.Allocate();
        var slotB = factory.Allocate();

        var context = InitializationContext.WillInitialize
            .Descend( slotA )
            .Descend( slotB );

        Assert.True( context.IsHandled( slotA ) );
        Assert.True( context.IsHandled( slotB ) );
    }

    [Fact]
    public void Descend_NormalizesIntentToWillInitialize()
    {
        var context = default(InitializationContext).Descend();

        Assert.Equal( CallerIntent.WillInitialize, context.Intent );
    }

    [Fact]
    public void Descend_PreservesMetadata()
    {
        var metadata = InitializationMetadata.Modify;
        var context = InitializationContext.Create( metadata ).Descend();

        Assert.Same( metadata, context.Metadata );
    }

    [Fact]
    public void SlotAllocate_Returns32UniqueSlots()
    {
        var factory = new InitializationSlotFactory();
        var masks = new uint[32];

        for ( var i = 0; i < 32; i++ )
        {
            masks[i] = factory.Allocate().Mask;
        }

        for ( var i = 0; i < 32; i++ )
        {
            Assert.Equal( 1u << i, masks[i] );
        }
    }

    [Fact]
    public void SlotAllocate_ThrowsAfter32()
    {
        var factory = new InitializationSlotFactory();

        for ( var i = 0; i < 32; i++ )
        {
            factory.Allocate();
        }

        Assert.Throws<InvalidOperationException>( () => factory.Allocate() );
    }

    [Fact]
    public void OnConstructed_SlotIsNonZero()
    {
        Assert.NotEqual( 0u, InitializationSlot.OnConstructed.Mask );
    }

    [Fact]
    public void SlotCombine_OrOperator()
    {
        var factory = new InitializationSlotFactory();
        var slotA = factory.Allocate();
        var slotB = factory.Allocate();
        var combined = slotA | slotB;

        Assert.Equal( slotA.Mask | slotB.Mask, combined.Mask );
    }

    [Fact]
    public void CreateWithMetadata_StoresMetadata()
    {
        var metadata = InitializationMetadata.Default;
        var context = InitializationContext.Create( metadata );

        Assert.Same( metadata, context.Metadata );
    }

    [Fact]
    public void CreateWithMetadata_HasWillInitializeIntent()
    {
        var context = InitializationContext.Create( InitializationMetadata.Default );

        Assert.Equal( CallerIntent.WillInitialize, context.Intent );
        Assert.True( context.WillCallOnInitialized );
    }

    [Fact]
    public void WithInitialize_PassesWillInitializeIntent()
    {
        var obj = new RecordingInitializable();

        var result = obj.WithInitialize();

        Assert.Same( obj, result );
        Assert.Equal( CallerIntent.WillInitialize, obj.ReceivedContext.Intent );
        Assert.True( obj.ReceivedContext.WillCallOnInitialized );
    }

    [Fact]
    public void WithInitialize_WithMetadata_FlowsMetadata()
    {
        var obj = new RecordingInitializable();

        obj.WithInitialize( InitializationMetadata.Modify );

        Assert.Same( InitializationMetadata.Modify, obj.ReceivedContext.Metadata );
        Assert.Equal( CallerIntent.WillInitialize, obj.ReceivedContext.Intent );
    }

    private sealed class RecordingInitializable : IInitializable
    {
        public InitializationContext ReceivedContext { get; private set; }

        public void Initialize( InitializationContext context ) => this.ReceivedContext = context;
    }

    [Fact]
    public void DefaultContext_HasNullMetadata()
    {
        var context = default(InitializationContext);

        Assert.Null( context.Metadata );
    }

    [Fact]
    public void StaticDefault_EquivalentToDefaultStruct()
    {
        Assert.Equal( CallerIntent.None, InitializationContext.Default.Intent );
        Assert.False( InitializationContext.Default.WillCallOnInitialized );
        Assert.Null( InitializationContext.Default.Metadata );
    }
}
