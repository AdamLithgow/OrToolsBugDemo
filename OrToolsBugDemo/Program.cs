using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace VrpBugDemo;

class Program
{
	static void Main(string[] args)
	{
		#region Setup

		long[,] timeMatrix =
		{
			{ 0, 700, 730, 325 },
			{ 700, 0, 36, 380 },
			{ 730, 36, 0, 408 },
			{ 325, 380, 408, 0 }
		};

		long[,] timeWindows =
		{
			{ 1721638800, 1721665800 },
			{ 1721638800, 1721665800 },
			{ 1721649252, 1721652792 },
			{ 1721651640, 1721653440 },
		};

		int[][] pickupDropOffs = [[2, 3]];

		var manager = new RoutingIndexManager(timeMatrix.GetLength(0), 1, [0], [1]);
		var routing = new RoutingModel(manager);

		var solver = routing.solver();

		#endregion

		#region Time Constraints

		var transitTimeCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
		{
			var fromNode = manager.IndexToNode(fromIndex);
			var toNode = manager.IndexToNode(toIndex);

			return timeMatrix[fromNode, toNode];
		});

		routing.SetArcCostEvaluatorOfAllVehicles(transitTimeCallbackIndex);

		var waitingTime = (long)new TimeSpan(30, 30, 0).TotalSeconds;

		routing.AddDimensionWithVehicleCapacity(
			transitTimeCallbackIndex,						// transit callback
			waitingTime,									// allow waiting time
			[1721665800],									// vehicle maximum capacities - this is the vehicle end time
			false,						// start cumul to zero - determines if the cumulative variable is set to zero at the start of each vehicle's route
			"Time"
		);

		var timeDimension = routing.GetMutableDimension("Time");

		for (var i = 0; i < manager.GetNumberOfVehicles(); ++i)
		{
			routing.AddVariableMaximizedByFinalizer(timeDimension.CumulVar(routing.Start(i)));
			routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.End(i)));
		}

		#endregion

		#region Pickup and Delivery Constraints

		for (var i = 0; i < pickupDropOffs.GetLength(0); i++)
		{
			var pickupIndex = manager.NodeToIndex(pickupDropOffs[i][0]);
			var deliveryIndex = manager.NodeToIndex(pickupDropOffs[i][1]);

			routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
			solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
			solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex), timeDimension.CumulVar(deliveryIndex)));
		}

		#endregion

		#region Time Windows

		for (var i = 2 * manager.GetNumberOfVehicles(); i < timeWindows.GetLength(0); ++i)
		{
			var index = manager.NodeToIndex(i);

			timeDimension.CumulVar(index).SetRange(timeWindows[i, 0], timeWindows[i, 1]);
			timeDimension.SetCumulVarSoftUpperBound(index, timeWindows[i, 0], 1);
		}

		for (var i = 0; i < manager.GetNumberOfVehicles(); ++i)
		{
			var startIndex = routing.Start(i);
			var startNode = manager.IndexToNode(startIndex);

			var endIndex = routing.End(i);
			var endNode = manager.IndexToNode(endIndex);

			timeDimension.CumulVar(startIndex).SetRange(timeWindows[startNode, 0], timeWindows[startNode, 1]);
			timeDimension.CumulVar(endIndex).SetRange(timeWindows[endNode, 0], timeWindows[endNode, 1]);
		}

		#endregion

		#region Penalties

		for (var i = 2 * manager.GetNumberOfVehicles(); i < timeMatrix.GetLength(0); i++)
		{
			var locationIndex = manager.NodeToIndex(i);
			routing.AddDisjunction([locationIndex], 10000);
		}

		#endregion

		#region Break Constraints

		for (var i = 0; i < manager.GetNumberOfVehicles(); i++)
		{
			IntervalVar breakVarOfVehicle = solver.MakeFixedDurationIntervalVar(
				start_min: 1721646300,
				start_max: 1721649600,
				duration: 1800,
				optional: false,
				name: $"Route{i}Break"
			);

			timeDimension.SetBreakIntervalsOfVehicle(new IntervalVarVector([breakVarOfVehicle]), i, -1, -1);
		}

		#endregion

		#region Solve

		try
		{
			var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
			searchParameters.TimeLimit = new Duration { Seconds = 300 };
			searchParameters.LogSearch = true;

			var solution = routing.SolveWithParameters(searchParameters);

			if (solution != null)
			{
				Console.WriteLine(GetSolveStatusMessage(routing.GetStatus()));
				PrintSolution(manager.GetNumberOfVehicles(), routing, manager, solution);
			}
			else
			{
				Console.WriteLine(GetSolveStatusMessage(routing.GetStatus()));
				throw new ApplicationException("No solution was found");
			}

		}
		catch (Exception ex)
		{
			Console.WriteLine($"An exception was thrown while performing a solve/print: {ex}");
			throw;
		}

		#endregion
	}

	private static void PrintSolution(in int vehicleNumber, in RoutingModel routing, in RoutingIndexManager manager,
		in Assignment solution)
	{
		Console.WriteLine($"Objective: {solution.ObjectiveValue()}");

		string droppedNodes = "Dropped nodes:";
		for (int index = 0; index < routing.Size(); ++index)
		{
			if (routing.IsStart(index) || routing.IsEnd(index))
			{
				continue;
			}
			if (solution.Value(routing.NextVar(index)) == index)
			{
				droppedNodes += " " + manager.IndexToNode(index);
			}
		}
		Console.WriteLine("{0}", droppedNodes);
		Console.WriteLine();

		RoutingDimension timeDimension = routing.GetMutableDimension("Time");
		decimal totalDistance = 0;
		TimeSpan totalTime = new();
		for (int i = 0; i < vehicleNumber; ++i)
		{
			Console.WriteLine($"Route {i}:");
			decimal routeDistance = 0;
			var index = routing.Start(i);

			var startTimeVar = timeDimension.CumulVar(index);

			long previousMinTime = 0;
			long previousMaxTime = 0;

			while (true)
			{
				var timeVar = timeDimension.CumulVar(index);
				long nodeIndex = manager.IndexToNode(index);

				foreach (var breakWindow in timeDimension.GetBreakIntervalsOfVehicle(i))
				{
					if (breakWindow.StartMin() <= solution.Min(timeVar) && breakWindow.StartMin() >= previousMaxTime)
						Console.WriteLine($"Break start: {ConvertToDateTime(breakWindow.StartMin())}");
					if (breakWindow.EndMax() <= solution.Max(timeVar) && breakWindow.EndMax() >= previousMinTime)
						Console.WriteLine($"Break end: {ConvertToDateTime(breakWindow.EndMax())}");

					previousMinTime = solution.Min(timeVar);
					previousMaxTime = solution.Max(timeVar);
				}

				var arrivalTime = ConvertToDateTime(solution.Min(timeVar));
				var departureTime = ConvertToDateTime(solution.Max(timeVar));
				var elapsedTime = departureTime - arrivalTime;

				Console.WriteLine($"{nodeIndex} -> ");
				Console.WriteLine($"    Arrival: {arrivalTime}");
				Console.WriteLine($"    Departure: {departureTime}");
				Console.WriteLine($"    Elapsed: {elapsedTime}");

				if (routing.IsEnd(index))
					break;

				var previousIndex = index;
				index = solution.Value(routing.NextVar(index));
				var metersDistance = routing.GetArcCostForVehicle(previousIndex, index, i) * (decimal)16;
				decimal milesDistance = Math.Round(metersDistance / 1609, 2);
				routeDistance += milesDistance;
			}
			var endTimeVar = timeDimension.CumulVar(index);

			var routeEndTime = ConvertToDateTime(solution.Min(endTimeVar));
			var routeStartTime = ConvertToDateTime(solution.Min(startTimeVar));
			var elapsedRouteTime = routeEndTime - routeStartTime;

			Console.WriteLine($"Distance of the route: {routeDistance}m");
			Console.WriteLine($"Time of the route: {elapsedRouteTime}");
			Console.WriteLine();
			totalTime = totalTime.Add(elapsedRouteTime);
			totalDistance += routeDistance;
		}
		Console.WriteLine();
		Console.WriteLine($"Total Distance of all routes: {totalDistance}m", totalDistance);
		Console.WriteLine($"Total time of all routes: {totalTime}");
	}

	private static DateTime ConvertToDateTime(long unixSeconds)
	{
		return DateTimeOffset.FromUnixTimeMilliseconds(unixSeconds * 1000).DateTime;
	}

	private static string GetSolveStatusMessage(int orToolsSolveStatus)
	{
		return orToolsSolveStatus switch
		{
			0 => "ROUTING_NOT_SOLVED",
			1 => "ROUTING_SUCCESS",
			2 => "ROUTING_PARTIAL_SUCCESS_LOCAL_OPTIMUM_NOT_REACHED",
			3 => "ROUTING_FAIL",
			4 => "ROUTING_FAIL_TIMEOUT",
			5 => "ROUTING_INVALID",
			6 => "ROUTING_INFEASIBLE",
			7 => "ROUTING_OPTIMAL",
			_ => "UNKNOWN_STATUS"
		};
	}
}