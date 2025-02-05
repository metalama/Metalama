[Introduction]
internal class TargetClass : DerivedClass
{
  public static new event global::System.EventHandler BaseClassEvent
  {
    add
    {
      // Call base class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.BaseClass.BaseClassEvent += value;
    }
    remove
    {
      // Call base class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.BaseClass.BaseClassEvent -= value;
    }
  }
  public static new event global::System.EventHandler BaseClassEventHiddenByEvent
  {
    add
    {
      // Call derived class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.DerivedClass.BaseClassEventHiddenByEvent += value;
    }
    remove
    {
      // Call derived class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.DerivedClass.BaseClassEventHiddenByEvent -= value;
    }
  }
  public static new event global::System.EventHandler DerivedClassEvent
  {
    add
    {
      // Call derived class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.DerivedClass.DerivedClassEvent += value;
    }
    remove
    {
      // Call derived class event.
      global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic.DerivedClass.DerivedClassEvent -= value;
    }
  }
  public static event global::System.EventHandler NonExistentEvent
  {
    add
    {
    // Do nothing.
    }
    remove
    {
    // Do nothing.
    }
  }
}