using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class MeshAmbisonics : AmbiBase<WyrmAmbisonicContributor>
{
    protected override int AllocatedCapacity => throw new System.NotImplementedException();

    public AmbisonicGeneratorType _Type;

    public AmbisonicGeneratorType Type
    {
        get => _Type;
        set
        {
            _Type = value;

            if (IsRegistered)
                Types[SoAIndex] = (byte)value;
        }
    }

    public MeshFilter meshFilter;

    void OnEnable()
    {
        meshFilter = this.EnsureReference(meshFilter);
    }

    public Vector3 _VirtualPosition;
    public Vector3 _VirtualDirection;
    public float _HorizontalBlur;
    public float _VerticalBlur;

    public Vector3 VirtualPosition
    {
        get => _VirtualPosition;
        set
        {
            _VirtualPosition = value;

            if (IsRegistered)
                VirtualPositions[SoAIndex] = value;
        }
    }

    public Vector3 VirtualDirection
    {
        get => _VirtualDirection;
        set
        {
            _VirtualDirection = value;

            if (IsRegistered)
                VirtualDirections[SoAIndex] = value;
        }
    }
    public float HorizontalBlur
    {
        get => _HorizontalBlur;
        set
        {
            _HorizontalBlur = value;

            if (IsRegistered)
                HorizontalBlurs[SoAIndex] = value;
        }
    }
    public float VerticalBlur
    {
        get => _VerticalBlur;
        set
        {
            value = Mathf.Clamp01(0, 1, value);
            _VerticalBlur = value;

            if (IsRegistered)
                VerticalBlurs[SoAIndex] = value;
        }
    }


    public static NativeArray<vector3> VirtualPosition;
    public static NativeArray<vector3> VirtualDirection;
    public static NativeArray<float> HorizontalBlurs;
    public static NativeArray<float> VerticalBlurs;

    protected override void AllocateNative()
    {
        VirtualPositions = new(AllocatedCapacity, Allocator.Persistent);
        VirtualDirections = new(AllocatedCapacity, Allocator.Persistent);
        HorizontalBlurs = new(AllocatedCapacity, Allocator.Persistent);
        VerticalBlurs = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        VirtualPositions.TryDispose();
        VirtualDirections.TryDispose();
        HorizontalBlurs.TryDispose();
        VerticalBlurs.TryDispose();
    }

    protected override void LoadObjectToArrays()
    {
        VirtualPositions[SoAIndex] = VirtualPosition;
        VirtualDirections[SoAIndex] = VirtualDirection;
        HorizontalBlurs[SoAIndex] = HorizontalBlur;
        VerticalBlurs[SoAIndex] = VerticalBlur;
    }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            VirtualPositions[removedIndex] = VirtualPositions[lastIndex];
            VirtualDirections[removedIndex] = VirtualDirections[lastIndex];
            HorizontalBlurs[removedIndex] = HorizontalBlurs[lastIndex];
            VerticalBlurs[removedIndex] = VerticalBlurs[lastIndex];
        }
    }
}


public enum AmbisonicGeneratorType
{
    Positional,
    PositionalBlurrable,
    Directional,
    DirectionalBlurrable,
    Cube,
    Mesh,
    Custom
}