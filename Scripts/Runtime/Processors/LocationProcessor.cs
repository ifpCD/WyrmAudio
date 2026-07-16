using Unity.Jobs;

internal static class LocationProcessor
{
    public static JobHandle ScheduleLocation(
        GraphManager graphProvider,
        JobHandle? appendTo = default)
    {
        if (WyrmPoolController.Instance.ActiveCount == 0)
            return default;

        var listener = WyrmAudioManager.GetAudioListener();
        if (listener == null)
            return default;

        var ListenerPosition = listener.transform.position;

        var locateListenerJob = new LocateListenerJob
        {
            ListenerPosition = ListenerPosition,

            ShapeWorldToLocal = graphProvider.ShapeWorldToLocal,
            ShapeExtents = graphProvider.ShapeExtents,
            ShapeRoomIdentifier = graphProvider.ShapeRoomIdentifier,

            PortalWorldToLocal = graphProvider.PortalWorldToLocal,
            PortalExtents = graphProvider.PortalExtents,
            PortalRoomA = graphProvider.PortalRoomA,
            PortalRoomB = graphProvider.PortalRoomB,

            ListenerRoomIdentifier = graphProvider.ListenerRoomIdentifier
        };
        JobHandle locateListenerHandle = locateListenerJob.Schedule(dependsOn: appendTo ?? default);

        var locateSourcesJob = new LocateSourcesJob
        {
            SourcePositions = WyrmPoolController.Instance.SourcePositions,

            ShapeWorldToLocal = graphProvider.ShapeWorldToLocal,
            ShapeExtents = graphProvider.ShapeExtents,
            ShapeRoomIdentifier = graphProvider.ShapeRoomIdentifier,

            PortalWorldToLocal = graphProvider.PortalWorldToLocal,
            PortalExtents = graphProvider.PortalExtents,
            PortalRoomA = graphProvider.PortalRoomA,
            PortalRoomB = graphProvider.PortalRoomB,
            
            SourceRoomIdentifiers = WyrmPoolController.Instance.SourceRoomIdentifiers
        };
        JobHandle locateSourcesHandle = locateSourcesJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, dependsOn: appendTo ?? default);

        return JobHandle.CombineDependencies(locateListenerHandle, locateSourcesHandle);
    }
}