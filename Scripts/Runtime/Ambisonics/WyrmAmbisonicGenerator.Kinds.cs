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

    bool AttachmentIsStale => _attachedType != _type || IsMeshAttachmentStale();

    bool IsMeshAttachmentStale()
    {
        if (_type != AmbisonicGeneratorType.Mesh)
            return false;

        if (_attachedMeshTarget != _meshTarget)
            return true;

        if (_attachedMesh != ProjectableMesh)
            return true;

        if (_attachedVertexColorBands != _meshVertexColorBands)
            return true;

        return false;
    }

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
