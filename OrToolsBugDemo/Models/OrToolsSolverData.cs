using VrpBugDemo.Enums;

namespace VrpBugDemo.Models;

public class OrToolsSolverData
{
	public int NumberOfRoutes { get; set; }
	public long[,] TimeMatrix { get; set; } = {};
	public long[,] DistanceMatrix { get; set; } = {};
	public int[] StartLocations { get; set; } = [];
	public int[] EndLocations { get; set; } = [];
	public int[][] PickupDropOffs { get; set; } = [];
	public long[,] TimeWindows { get; set; } = {};
	public Dictionary<int, long[]> CapacityDemands { get; set; } = [];
	public Dictionary<int, long[]> VehicleCapacities { get; set; } = [];
	public long[] VehicleTimeCapacities { get; set; } = [];
	public List<Location> Locations { get; set; } = [];
	public long[] RouteMaxDurations { get; set; } = [];
	public long[][,] ArcCostMatrix { get; set; } = [];
	public int[] OrderPriorityType = [];
}