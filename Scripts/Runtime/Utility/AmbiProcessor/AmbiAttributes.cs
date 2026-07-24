using System;

public class AmbiSynchronizable : Attribute { }

// public class AmbiSync : Attribute
// {
//     public string FieldName { get; }
//     public AmbiSyncType SyncType { get; }

//     public AmbiSync(string fieldName, AmbiSyncType syncType = AmbiSyncType.Input)
//     {
//         FieldName = fieldName;
//         SyncType = syncType;
//     }
// }

public class AmbiRegistrationHook : Attribute { }
public class AmbiDeregistrationHook : Attribute { }

public class AmbiCallback : Attribute
{
    public AmbiManagedHookType ManagedCallback { get; }

    public AmbiCallback(AmbiManagedHookType managedCallback = AmbiManagedHookType.PreBatchUpdate)
    {
        ManagedCallback = managedCallback;
    }
}
