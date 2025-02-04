using Google.OrTools.ConstraintSolver;
using VrpBugDemo.Enums;
using VrpBugDemo.Models;
using VrpBugDemo.Models.Request;
using VrpBugDemo.Models.Response;
using VrpBugDemo.Utilities;

namespace VrpBugDemo.DataMappers;

public static class MapToVrpResponse
{
	public static VrpSolverResponse Map(in VrpSolverRequest request, OrToolsSolverData solverData, in RoutingModel routing, in RoutingIndexManager manager, in Assignment? solution, TimeConverter timeConverter)
	{
		var unassignedLocations = new List<int>();
		var allOrders = new List<OrderRequest>();
		allOrders.AddRange(request.Orders);
		allOrders.AddRange(request.Routes.SelectMany(route => route.Orders));

		for (int index = 0; index < routing.Size(); ++index)
		{
			if (routing.IsStart(index) || routing.IsEnd(index))
				continue;
			if (solution == null || solution.Value(routing.NextVar(index)) == index)
				unassignedLocations.Add(manager.IndexToNode(index));
		}

		var unassignableOrders = unassignedLocations.Select(unassignedLocation => solverData.Locations.ElementAt(unassignedLocation))
			.Select(lookedUpLocation => new OrderResponse
			{
				OrderId = OrderSearch.FindOrder(allOrders, order => order.Location.LocationId == lookedUpLocation.LocationId)!.OrderId,
				Location = new Location
				{
					LocationId = lookedUpLocation.LocationId,
					Sequence = null,
					Coordinate = lookedUpLocation.Coordinate,
					StartTime = lookedUpLocation.StartTime,
					EndTime = lookedUpLocation.EndTime,
					LoadingTime = lookedUpLocation.LoadingTime,
					UnloadingTime = lookedUpLocation.UnloadingTime,
				},
			})
			.ToList();

		if (solution == null)
		{
			return new VrpSolverResponse
			{
				Routes = request.Routes.Select(route =>
					new RouteResponse
					{
						RouteId = route.RouteId,
						StartDepot = route.StartDepot,
						EndDepot = route.EndDepot,
					}
				).ToList(),
				UnassignableOrders = unassignableOrders,
				Status = SolveStatus.GetSolveStatusMessage(routing.GetStatus()),
			};
		}

		var routeResponses = new List<RouteResponse>();
		var timeDimension = routing.GetMutableDimension("Time");

		for (var i = 0; i < request.Routes.Count; i++)
		{
			var index = routing.Start(i);
			var lastIndex = index;
			var routeRequest = request.Routes.ElementAt(i);
			var sequence = 0;

			var routeResponse = new RouteResponse
			{
				RouteId = routeRequest.RouteId,
				StartDepot = routeRequest.StartDepot,
				EndDepot = routeRequest.EndDepot,
			};

			if (solution == null)
			{
				routeResponses.Add(routeResponse);
				continue;
			}

			long waitTimeAfterLastNode = 0;

			while (true)
			{
				var timeVar = timeDimension.CumulVar(index);
				var lookedUpLocation = solverData.Locations.ElementAt(manager.IndexToNode(index));

				var location = new Location
				{
					LocationId = lookedUpLocation.LocationId,
					Sequence = sequence,
					Coordinate = lookedUpLocation.Coordinate,
					StartTime = timeConverter.ConvertToDateTime(solution.Min(timeVar) - (long)((lookedUpLocation.LoadingTime?.TotalSeconds ?? 0) + (lookedUpLocation.UnloadingTime?.TotalSeconds ?? 0)) - waitTimeAfterLastNode).ToLocalTime(),
					EndTime = timeConverter.ConvertToDateTime(solution.Max(timeVar)).ToLocalTime(),
					LoadingTime = lookedUpLocation.LoadingTime,
					UnloadingTime = lookedUpLocation.UnloadingTime,
				};

				var correspondingOrder = OrderSearch.FindOrder(allOrders, order => order.Location.LocationId == lookedUpLocation.LocationId);

				if (correspondingOrder == null)
				{
					if (routeRequest.StartDepot.LocationId == lookedUpLocation.LocationId)
						routeResponse.StartDepot = location;
					else if (routeRequest.EndDepot.LocationId == lookedUpLocation.LocationId)
						routeResponse.EndDepot = location;
				}
				else
				{
					var lastNode = manager.IndexToNode(lastIndex);
					var node = manager.IndexToNode(index);

					routeResponse.Orders.Add(
						new OrderResponse
						{
							OrderId = correspondingOrder.OrderId,
							Location = location,
							DistanceSinceLastOrder = solverData.DistanceMatrix[lastNode, node],
							TimeSinceLastOrder = TimeSpan.FromSeconds(solverData.TimeMatrix[lastNode, node]),
							WaitTime = TimeSpan.FromSeconds(waitTimeAfterLastNode),
						}
					);
				}

				if (routing.IsEnd(index)) break;
				var slackVar = timeDimension.SlackVar(index);
				if (slackVar != null)
					waitTimeAfterLastNode = solution.Max(slackVar);
				sequence++;
				lastIndex = index;
				index = solution.Value(routing.NextVar(index));
			}

			routeResponses.Add(routeResponse);
		}

		return new VrpSolverResponse
		{
			Routes = routeResponses,
			UnassignableOrders = unassignableOrders,
			Status = SolveStatus.GetSolveStatusMessage(routing.GetStatus()),
		};
	}
}