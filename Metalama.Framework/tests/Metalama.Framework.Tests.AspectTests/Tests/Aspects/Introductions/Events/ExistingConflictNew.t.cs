[Introduction]
internal class TargetClass : DerivedClass
{
  public new event global::System.EventHandler BaseClassAbstractEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassAbstractEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassAbstractEvent -= value;
    }
  }
  public new event global::System.EventHandler BaseClassAbstractSealedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassAbstractSealedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassAbstractSealedEvent -= value;
    }
  }
  public new event global::System.EventHandler BaseClassEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassEvent -= value;
    }
  }
  public new event global::System.EventHandler BaseClassVirtualEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualEvent -= value;
    }
  }
  public new event global::System.EventHandler BaseClassVirtualOverridenEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualOverridenEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualOverridenEvent -= value;
    }
  }
  public new event global::System.EventHandler BaseClassVirtualSealedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualSealedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.BaseClassVirtualSealedEvent -= value;
    }
  }
  public new event global::System.EventHandler DerivedClassEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassEvent -= value;
    }
  }
  public new event global::System.EventHandler DerivedClassVirtualEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassVirtualEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassVirtualEvent -= value;
    }
  }
  public new event global::System.EventHandler DerivedClassVirtualSealedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassVirtualSealedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.DerivedClassVirtualSealedEvent -= value;
    }
  }
  public new event global::System.EventHandler HiddenBaseClassEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenBaseClassEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenBaseClassEvent -= value;
    }
  }
  public new event global::System.EventHandler HiddenBaseClassVirtualEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenBaseClassVirtualEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenBaseClassVirtualEvent -= value;
    }
  }
  public new event global::System.EventHandler HiddenVirtualBaseClassVirtualEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenVirtualBaseClassVirtualEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.HiddenVirtualBaseClassVirtualEvent -= value;
    }
  }
  public event global::System.EventHandler NonExistentEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
  }
}