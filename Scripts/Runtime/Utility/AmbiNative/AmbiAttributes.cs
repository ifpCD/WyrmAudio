using System;

public class AmbiSynchronizable : Attribute { }

public class AmbiSync : Attribute
{
    public string FieldName { get; }
    public AmbiSyncType SyncType { get; }

    public AmbiSync(string fieldName, AmbiSyncType syncType = AmbiSyncType.Input)
    {
        FieldName = fieldName;
        SyncType = syncType;
    }
}

public class AmbiProcessorHook : Attribute { }

public class AmbiManagedHook : Attribute
{
    public AmbiManagedHookType ManagedCallback { get; }

    public AmbiManagedHook(AmbiManagedHookType managedCallback = AmbiManagedHookType.PreBatchUpdate)
    {
        ManagedCallback = managedCallback;
    }
}
