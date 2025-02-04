using VrpBugDemo.Enums;

namespace VrpBugDemo.Models.Response;

public class VrpSolverResponse
{
	public List<RouteResponse> Routes { get; init; } = [];
	public List<OrderResponse> UnassignableOrders { get; init; } = [];
	public SolveStatus Status { get; init; } = SolveStatus.Unknown;
}