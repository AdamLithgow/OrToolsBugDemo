namespace VrpBugDemo.Models.Response;

public class OrderResponse
{
	/// <summary>
	/// The ID of the <see cref="OrderResponse">Route</see>.
	/// </summary>
	public string OrderId { get; init; }
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="OrderResponse">Order</see> occurs at.
	/// </summary>
	public Location Location { get; init; } = new();
	/// <summary>
	/// The distance between the last <see cref="OrderResponse">Order</see> and this one, measured in meters.
	/// </summary>
	public double DistanceSinceLastOrder { get; init; }
	/// <summary>
	/// The time between the departure of the last <see cref="OrderResponse">Order</see>, and the arrival at the current <see cref="OrderResponse">Order</see>.
	/// </summary>
	public TimeSpan TimeSinceLastOrder { get; init; }
	/// <summary>
	/// The time that the <see cref="RouteResponse">Route</see> was inactive at an <see cref="OrderResponse">Order</see> waiting.
	/// </summary>
	public TimeSpan WaitTime { get; init; }
}