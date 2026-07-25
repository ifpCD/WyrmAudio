using UnityEngine;

public static class BehaviourExtensions
{
    public static T EnsureReference<T>(this Component component, T reference)
        where T : Component
    {
        return reference ?? component.GetComponent<T>();
    }
}
