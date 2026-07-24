using Unity.Jobs;

internal static class LocationProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int sourceCount = WyrmBaseSource.EnabledInstanceCount;
        if (sourceCount == 0 || WyrmListener.EnabledInstanceCount == 0)
            return dependency;

        var locateListener = new LocateListenerJob
        { 
            ShapeWorldToLocal = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier = WyrmRoomShape.ShapeRoomIdentifier,
            ShapeCount = WyrmRoomShape.EnabledInstanceCount,

            PortalWorldToLocal = WyrmPortal.PortalWorldToLocal,
            PortalExtents = WyrmPortal.PortalExtents,
            PortalRoomA = WyrmPortal.PortalRoomA,
            PortalRoomB = WyrmPortal.PortalRoomB,
            PortalCount = WyrmPortal.EnabledInstanceCount,

            ListenerPosition = WyrmListener.ListenerPosition.Value,

            ListenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier,
        };
        JobHandle listenerHandle = locateListener.Schedule(dependency);

        var locateSources = new LocateSourcesJob
        {
            ShapeWorldToLocal = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier = WyrmRoomShape.ShapeRoomIdentifier,
            ShapeCount = WyrmRoomShape.EnabledInstanceCount,

            PortalWorldToLocal = WyrmPortal.PortalWorldToLocal,
            PortalExtents = WyrmPortal.PortalExtents,
            PortalRoomA = WyrmPortal.PortalRoomA,
            PortalRoomB = WyrmPortal.PortalRoomB,
            PortalCount = WyrmPortal.EnabledInstanceCount,

            SourcePositions = WyrmBaseSource.SourcePositions,
            
            SourceRoomIdentifiers = WyrmBaseSource.SourceRoomIdentifiers,
        };
        JobHandle sourcesHandle = locateSources.Schedule(sourceCount, 16, dependency);

        return JobHandle.CombineDependencies(listenerHandle, sourcesHandle);
    }
}
