using Unity.Jobs;

internal static class LocationProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int sourceCount = WyrmBaseSource.ActiveCount;
        if (WyrmBaseSource.ActiveCount == 0 || WyrmListener.ActiveCount == 0 || WyrmRoomShape.ActiveCount == 0 || WyrmPortal.ActiveCount == 0)
            return dependency;

        // csharpier-ignore
        var locateListener = new LocateListenerJob
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
        };
        JobHandle listenerHandle = locateListener.Schedule(dependency);

        // csharpier-ignore
        var locateSources = new LocateSourcesJob
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

            SourcePositions       = WyrmBaseSource.SourcePositions,

            SourceRoomIdentifiers = WyrmBaseSource.SourceRoomIdentifiers,
        };
        JobHandle sourcesHandle = locateSources.Schedule(sourceCount, 16, dependency);

        return JobHandle.CombineDependencies(listenerHandle, sourcesHandle);
    }
}
