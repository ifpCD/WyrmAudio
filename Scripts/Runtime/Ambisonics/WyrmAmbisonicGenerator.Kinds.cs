using UnityEngine;

public sealed partial class WyrmAmbisonicGenerator
{
    SimpleAmbisonics _simpleRow;
    MeshAmbisonics _meshRow;

    AmbisonicGeneratorType _attachedType;
    MeshFilter _attachedMeshTarget;
    Mesh _attachedMesh;
    bool _attachedVertexColorBands;

    Mesh ProjectableMesh => _meshTarget != null ? _meshTarget.sharedMesh : null;

    bool AttachmentIsStale =>
        _attachedType != _type
        || (
            _type == AmbisonicGeneratorType.Mesh
            && (_attachedMeshTarget != _meshTarget || _attachedMesh != ProjectableMesh || _attachedVertexColorBands != _meshVertexColorBands)
        );

    void AttachKind()
    {
        _attachedType = _type;
        _attachedMeshTarget = _meshTarget;
        _attachedMesh = ProjectableMesh;
        _attachedVertexColorBands = _meshVertexColorBands;

        if (_type == AmbisonicGeneratorType.Simple)
        {
            _simpleRow ??= new SimpleAmbisonics(this);
            _simpleRow.Register();
            return;
        }

        if (_attachedMesh == null)
            return;

        _meshRow ??= new MeshAmbisonics(this);
        _meshRow.Register();
    }

    void DetachKind()
    {
        _simpleRow?.Deregister();
        _meshRow?.Deregister();
    }

    void ReattachKind()
    {
        DetachKind();
        AttachKind();
    }
}
