using Unity.Jobs;

internal static class LocationProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int sourceCount = WyrmBaseSource.ActiveCount;
        if (
            WyrmBaseSource.CompletelyInactive
            || WyrmRoomShape.CompletelyInactive
            || WyrmPortal.CompletelyInactive
            || WyrmAudioManager.Listener == null
        )
            return dependency;

        // csharpier-ignore
        var locateListenerHandle = new LocateListenerJob
        {
            ShapeWorldToLocal      = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents           = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier    = WyrmRoomShape.ShapeRoomIdentifier,
            ShapeCount             = WyrmRoomShape.ActiveCount,

            PortalWorldToLocal     = WyrmPortal.PortalWorldToLocal,
            PortalExtents          = WyrmPortal.PortalExtents,
            PortalRoomA            = WyrmPortal.PortalRoomA,
            PortalRoomB            = WyrmPortal.PortalRoomB,
            PortalCount            = WyrmPortal.ActiveCount,

            ListenerPosition       = WyrmListener.ListenerPosition.Value,

            ListenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier,
        }.Schedule(dependency);

        // csharpier-ignore
        var locateSourcesHandle = new LocateSourcesJob
        {
            ShapeWorldToLocal     = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents          = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier   = WyrmRoomShape.ShapeRoomIdentifier,
            ShapeCount            = WyrmRoomShape.ActiveCount,

            PortalWorldToLocal    = WyrmPortal.PortalWorldToLocal,
            PortalExtents         = WyrmPortal.PortalExtents,
            PortalRoomA           = WyrmPortal.PortalRoomA,
            PortalRoomB           = WyrmPortal.PortalRoomB,
            PortalCount           = WyrmPortal.ActiveCount,

            SourcePositions       = WyrmBaseSource.Positions,

            SourceRoomIdentifiers = WyrmBaseSource.RoomIDs,
        }.Schedule(sourceCount, 16, dependency);

        return JobHandle.CombineDependencies(locateListenerHandle, locateSourcesHandle);
    }
}
