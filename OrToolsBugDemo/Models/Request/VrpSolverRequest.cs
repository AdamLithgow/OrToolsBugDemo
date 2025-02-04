namespace VrpBugDemo.Models.Request;

public class VrpSolverRequest
{
    /// <summary>
    /// The <see cref="RouteRequest"/>s that <see cref="OrderRequest"/>s will be assigned to.
    /// </summary>
	public List<RouteRequest> Routes { get; init; } = [];
	/// <summary>
	/// The <see cref="RouteRequest"/>s that need to be assigned to <see cref="OrderRequest"/>s.
	/// </summary>
	public List<OrderRequest> Orders { get; init; } = [];
}