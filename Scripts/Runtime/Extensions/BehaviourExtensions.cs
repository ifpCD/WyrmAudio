using UnityEngine;

public static class BehaviourExtensions
{
    public static T EnsureReference<T>(this Component component, T reference)
        where T : Component
    {
        if (reference != null)
            return reference;

        return component.GetComponent<T>() ?? component.gameObject.AddComponent<T>();
    }
}
