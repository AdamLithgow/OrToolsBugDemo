using NetTopologySuite.Geometries;

namespace VrpBugDemo.Models.Request;

/// <summary>
/// Represents the combination of a vehicle and driver for a particular period of time
/// </summary>
public class RouteRequest
{
	/// <summary>
	/// The ID of the <see cref="RouteRequest">Route</see>.
	/// </summary>
	public string RouteId { get; init; } = Guid.NewGuid().ToString();
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="RouteRequest">Route</see> will start at.
	/// </summary>
	public Location StartDepot { get; init; } = new();
	/// <summary>
	/// The <see cref="Location"/> that the <see cref="RouteRequest">Route</see> will end at.
	/// </summary>
	public Location EndDepot { get; init; } = new();
	/// <summary>
	/// A collection of <see cref="OrderRequest">Order</see>s that are already assigned to the <see cref="RouteRequest">Route</see>
	/// </summary>
	public List<OrderRequest> Orders { get; init; } = [];
	/// <summary>
	/// A collection of <see cref="MultiPolygon"/>. Each geo json can contain one or many route zones. An empty collection indicates that there are no geographic constraints on any.
	/// </summary>
	public List<MultiPolygon> RouteZones { get; init; } = [];
	public TimeSpan ArriveDepartDelay { get; init; }
}