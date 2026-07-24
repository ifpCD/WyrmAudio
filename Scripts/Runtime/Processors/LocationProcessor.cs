using Unity.Jobs;

internal static class LocationProcessor
{
    public static JobHandle ScheduleLocation(GraphManager graphProvider, JobHandle? appendTo = default)
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

            ShapeWorldToLocal = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier = WyrmRoomShape.ShapeRoomIdentifier,

            PortalWorldToLocal = WyrmPortal.PortalWorldToLocal,
            PortalExtents = WyrmPortal.PortalExtents,
            PortalRoomA = WyrmPortal.PortalRoomA,
            PortalRoomB = WyrmPortal.PortalRoomB,

            ListenerRoomIdentifier = graphProvider.ListenerRoomIdentifier,
        };
        JobHandle locateListenerHandle = locateListenerJob.Schedule(dependsOn: appendTo ?? default);

        var locateSourcesJob = new LocateSourcesJob
        {
            SourcePositions = WyrmPoolController.Instance.SourcePositions,

            ShapeWorldToLocal = WyrmRoomShape.ShapeWorldToLocal,
            ShapeExtents = WyrmRoomShape.ShapeExtents,
            ShapeRoomIdentifier = WyrmRoomShape.ShapeRoomIdentifier,

            PortalWorldToLocal = WyrmPortal.PortalWorldToLocal,
            PortalExtents = WyrmPortal.PortalExtents,
            PortalRoomA = WyrmPortal.PortalRoomA,
            PortalRoomB = WyrmPortal.PortalRoomB,

            SourceRoomIdentifiers = WyrmPoolController.Instance.SourceRoomIdentifiers,
        };
        JobHandle locateSourcesHandle = locateSourcesJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, dependsOn: appendTo ?? default);

        return JobHandle.CombineDependencies(locateListenerHandle, locateSourcesHandle);
    }
}
