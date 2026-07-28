using System;

public class AmbiSynchronizable : Attribute { }

public class AmbiSync : Attribute
{
    public string NativeArrayBackingField { get; }

    public AmbiSync(string nativeArrayBackingField)
    {
        NativeArrayBackingField = nativeArrayBackingField;
    }
}