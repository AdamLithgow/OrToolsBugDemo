namespace VrpBugDemo.Models.Request;

/// <summary>
/// Represents a desired stop along a <see cref="RouteRequest">Route</see>
/// </summary>
public class OrderRequest
{
	/// <summary>
	/// The ID of the <see cref="OrderRequest">Order</see>.
	/// </summary>
	public string OrderId { get; init; } = Guid.NewGuid().ToString();
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="OrderRequest">Order</see> occurs at.
	/// </summary>
	public Location Location { get; init; } = new();
	/// <summary>
	/// The next <see cref="OrderRequest">Order</see> that is to be visited. This can represent the drop-off point for demand/response, or the next stop on a fixed route.
	/// </summary>
	public OrderRequest? SubsequentOrder { get; init; }
}