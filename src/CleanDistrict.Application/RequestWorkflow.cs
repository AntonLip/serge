using CleanDistrict.Domain;

namespace CleanDistrict.Application;

public static class RequestWorkflow
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Transitions = new()
    {
        [RequestStatus.New] = [RequestStatus.Approved, RequestStatus.Cancelled],
        [RequestStatus.Approved] = [RequestStatus.Assigned, RequestStatus.Cancelled],
        [RequestStatus.Assigned] = [RequestStatus.InProgress, RequestStatus.Cancelled],
        [RequestStatus.InProgress] = [RequestStatus.Completed],
        [RequestStatus.Completed] = [RequestStatus.Closed],
        [RequestStatus.Closed] = [],
        [RequestStatus.Cancelled] = []
    };

    public static bool CanTransition(RequestStatus from, RequestStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
