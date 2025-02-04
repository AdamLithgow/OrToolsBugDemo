namespace VrpBugDemo.Models.Response;

public class RouteResponse
{
	/// <summary>
	/// The ID of the <see cref="RouteResponse">Route</see>.
	/// </summary>
	public string RouteId { get; init; }
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="RouteResponse">Route</see> will start at.
	/// </summary>
	public Location StartDepot { get; set; } = new();
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="RouteResponse">Route</see> will end at.
	/// </summary>
	public Location EndDepot { get; set; } = new();
	public List<OrderResponse> Orders { get; init; } = [];
}