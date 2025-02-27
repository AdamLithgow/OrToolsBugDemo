using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using NetTopologySuite.Geometries;
using VrpBugDemo.DataMappers;
using VrpBugDemo.Models;
using VrpBugDemo.Models.Request;
using VrpBugDemo.Models.Response;
using VrpBugDemo.Utilities;

namespace VrpBugDemo;

public class Solver
{
	public async Task<VrpSolverResponse> SolveRoutes(VrpSolverRequest request, OrToolsSolverData solverData, TimeConverter timeConverter)
	{
		#region IRRELEVANT

		var manager = new RoutingIndexManager(solverData.TimeMatrix.GetLength(0), solverData.NumberOfRoutes, solverData.StartLocations, solverData.EndLocations);
		var routing = new RoutingModel(manager);

		var solver = routing.solver();

		var transitTimeCallbacks = request.Routes.Select(route =>
		{
			return routing.RegisterTransitCallback((fromIndex, toIndex) =>
			{
				var fromNode = manager.IndexToNode(fromIndex);
				var toNode = manager.IndexToNode(toIndex);
				var toLocation = solverData.Locations.ElementAt(toNode);
				var fromLocation = solverData.Locations.ElementAt(fromNode);
				return solverData.TimeMatrix[fromNode, toNode] + (long)((toLocation.LoadingTime?.TotalSeconds ?? 0) + (toLocation.UnloadingTime?.TotalSeconds ?? 0)) + (solverData.TimeMatrix[fromNode, toNode] == 0 ? 0 : (long)route.ArriveDepartDelay.TotalSeconds) + (manager.IndexToNode(manager.GetStartIndex(request.Routes.IndexOf(route))) == fromNode ? (long)((fromLocation.LoadingTime?.TotalSeconds ?? 0) + (fromLocation.UnloadingTime?.TotalSeconds ?? 0)) : 0);
			});
		}).ToArray();

		routing.AddDimensionWithVehicleTransitAndCapacity(
			transitTimeCallbacks,
			30000,
			solverData.VehicleTimeCapacities,
			false,
			"Time"
		);

		var timeDimension = routing.GetMutableDimension("Time");

		for (var i = 0; i < solverData.NumberOfRoutes; ++i)
		{
			routing.AddVariableMaximizedByFinalizer(timeDimension.CumulVar(routing.Start(i)));
			routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.End(i)));
		}

		foreach (var capacity in solverData.VehicleCapacities)
		{
			routing.AddDimensionWithVehicleCapacity(
				routing.RegisterUnaryTransitCallback(fromIndex => solverData.CapacityDemands.GetValueOrDefault(capacity.Key, [])[manager.IndexToNode(fromIndex)]),
				0,
				capacity.Value,
				true,
				$"{capacity.Key}Capacity"
			);
		}

		#endregion

		#region Travel Constraints

		// Sets the cost for slack to a coefficient of 1. Without this, and with the utilization of
		// span upper bounds, slack does not seem to be properly minimized in the solve
		// TODO: Without this, the "AllOrdersAreAssignedAndTimesAreCorrect" test case fails due to excess wait time
		timeDimension.SetSpanCostCoefficientForAllVehicles(1);

		// Span upper bound has to be used on dimensions that have a slack so that the
		// slack gets counted as part of the total against the max. Without the span
		// upper bound, the slack basically gets ignored when calculating usage against max
		// TODO: Without this, the "RouteWithMaxDurationOfFortyFiveMinutesLeavesSixOrdersUnassigned" test case fails since route duration isn't being bounded
		for (var i = 0; i < solverData.NumberOfRoutes; i++)
		{
			timeDimension.SetSpanUpperBoundForVehicle(solverData.RouteMaxDurations[i], i);
		}

		// TODO: With both of the above, the "SolveWithRouteZonesAndOrdersOutsideAreaDropsOrders" test case fails because the depots are moved outside the acceptable bounds
		// The start/end times of the depot are placed outside of their hard constraints, and this should be marked is infeasible, but isn't.

		#endregion

		#region IRRELEVANT

		for (var i = 0; i < solverData.PickupDropOffs.GetLength(0); i++)
		{
			var pickupIndex = manager.NodeToIndex(solverData.PickupDropOffs[i][0]);
			var deliveryIndex = manager.NodeToIndex(solverData.PickupDropOffs[i][1]);
			routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
			solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
			solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex), timeDimension.CumulVar(deliveryIndex)));
		}

		for (var i = 2 * solverData.NumberOfRoutes; i < solverData.TimeWindows.GetLength(0); ++i)
		{
			var index = manager.NodeToIndex(i);
			timeDimension.CumulVar(index).SetRange(solverData.TimeWindows[i, 0], solverData.TimeWindows[i, 1]);

			routing.AddToAssignment(timeDimension.SlackVar(index));
			routing.AddVariableMinimizedByFinalizer(timeDimension.SlackVar(index));

			if (solverData.OrderPriorityType[i] == 0) timeDimension.SetCumulVarSoftUpperBound(index, solverData.TimeWindows[i, 0], 2);
			else timeDimension.SetCumulVarSoftLowerBound(index, solverData.TimeWindows[i, 1], 2);
		}

		for (var i = 0; i < solverData.NumberOfRoutes; ++i)
		{
			var startIndex = routing.Start(i);
			var startNode = manager.IndexToNode(startIndex);
			var endIndex = routing.End(i);
			var endNode = manager.IndexToNode(endIndex);
			timeDimension.CumulVar(startIndex).SetRange(solverData.TimeWindows[startNode, 0], solverData.TimeWindows[startNode, 1]);
			routing.AddToAssignment(timeDimension.SlackVar(startIndex));
			timeDimension.CumulVar(endIndex).SetRange(solverData.TimeWindows[endNode, 0], solverData.TimeWindows[endNode, 1]);
		}

		for (var i = 0; i < manager.GetNumberOfVehicles(); i++)
		{
			routing.SetVehicleUsedWhenEmpty(true, i);
		}

		#endregion

		#region Route Zones

		// Build the NetTopologySuite (NTS) GeometryService
		NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices(
			NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
			new PrecisionModel(1000d),
			4326,
			GeometryOverlay.NG,
			new CoordinateEqualityComparer()
		);

		var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);

		// Create a callback for each route
		var routeZoneCallbacks = request.Routes.Select(route =>
		{
			return routing.RegisterUnaryTransitCallback(fromIndex =>
			{
				// If there are no route zones, there's no point in doing any more logic
				if (route.RouteZones.Count == 0)
					return 0;

				var fromNode = manager.IndexToNode(fromIndex);
				// Get the location corresponding to the current node
				var location = solverData.Locations.ElementAt(fromNode);
				// Create a NTS point using the location's coordinates
				var point = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(location.Coordinate.Longitude, location.Coordinate.Latitude));

				// Check if any route zone assigned to the route can contain the point
					// TODO: Setting to true results in 2 expected, 0 actual
				// var orderIsServiceableByRoute = true;
					// TODO: Setting to false results in infeasible solve
				// var orderIsServiceableByRoute = false;
					// TODO: Performing evaluation results in time window being thrown off, but only when
					// SetSpanCostCoefficientForAllVehicles and/or SetSpanUpperBoundForVehicle are set
				var orderIsServiceableByRoute = route.RouteZones.Any(routeZone => routeZone.Contains(point));

				return orderIsServiceableByRoute ? 0 : 1;
			});
		});

		// Create the route zone dimension and set capacity to 0 so any
		// orders that aren't in the route zone (value of 1) get dropped
		routing.AddDimensionWithVehicleTransits(
			routeZoneCallbacks.ToArray(),
			0,
			0,
			true,
			"RouteZones"
		);

		#endregion

		#region IRRELEVANT

		for (var i = 2 * solverData.NumberOfRoutes; i < solverData.TimeMatrix.GetLength(0); i++)
		{
			var locationIndex = manager.NodeToIndex(i);
			routing.AddDisjunction([locationIndex], 20000);
		}

		for (var i = 0; i < manager.GetNumberOfVehicles(); i++)
		{
			var arcCostMatrix = solverData.ArcCostMatrix[i];
			routing.SetArcCostEvaluatorOfVehicle(routing.RegisterTransitCallback((long fromIndex, long toIndex) => arcCostMatrix[manager.IndexToNode(fromIndex), manager.IndexToNode(toIndex)]), i);
		}

		var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
		searchParameters.LogSearch = true;
		searchParameters.TimeLimit = new Duration { Seconds = 300 };
		var solution = routing.SolveWithParameters(searchParameters);
		var result = MapToVrpResponse.Map(request, solverData, routing, manager, solution, timeConverter);
		return result;

		#endregion
	}
}