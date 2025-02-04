using VrpBugDemo.Models.Request;

namespace VrpBugDemo.Models;

/// <summary>
/// Represents a place along a <see cref="RouteRequest"/>
/// </summary>
public class Location
{
	/// <summary>
	/// The ID of the <see cref="Location"/>.
	/// </summary>
	public string LocationId { get; init; }
	/// <summary>
	/// The order in which this <see cref="Location"/> should occur on the <see cref="RouteRequest"/>.
	/// Values do not need to be sequential. Null value indicates that the order does not matter.
	/// This property is ignored for start and end locations for <see cref="RouteRequest"/>s.
	/// </summary>
	public int? Sequence { get; init; }
	/// <summary>
	/// The latitude and longitude values of the <see cref="Location"/>
	/// </summary>
	public Coordinate Coordinate { get; init; } = new();
	/// <summary>
	/// The start time for a pickup/drop-off window in the case of an <see cref="OrderRequest"/>, or the availability start in the case of a <see cref="RouteRequest"/>.
	/// </summary>
	public DateTime StartTime { get; init; }
	/// <summary>
	/// The end time for a pickup/drop-off window in the case of an <see cref="OrderRequest"/>, or the availability end in the case of a <see cref="RouteRequest"/>.
	/// </summary>
	public DateTime EndTime { get; init; }
	/// <summary>
	/// The amount of time it takes to be able to operate at this <see cref="Location"/>. This could be the loading time for an <see cref="OrderRequest"/>, or the vehicle prep time for a <see cref="RouteRequest"/>.
	/// A null value indicates no prep time.
	/// </summary>
	public TimeSpan? LoadingTime { get; init; }
	/// <summary>
	/// The amount of time it before this <see cref="Location"/> can be left. This could be the unloading time for an <see cref="OrderRequest"/>, or the vehicle cleanup after a <see cref="RouteRequest"/>.
	/// A null value indicates no prep time.
	/// </summary>
	public TimeSpan? UnloadingTime { get; init; }
}