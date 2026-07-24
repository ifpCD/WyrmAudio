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

public class AmbiBatchHook : Attribute { }

public class AmbiHook : Attribute
{
    public AmbiManagedHookType ManagedCallback { get; }

    public AmbiHook(AmbiManagedHookType managedCallback = AmbiManagedHookType.PreBatchUpdate)
    {
        ManagedCallback = managedCallback;
    }
}
