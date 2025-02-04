using System.Text.Json;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using VrpBugDemo;
using VrpBugDemo.Models;
using VrpBugDemo.Models.Request;
using VrpBugDemo.Models.Response;
using VrpBugDemo.Utilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SolverTests;

public class SolverTests
{
	[Fact]
	public async Task SolveWithRouteZonesAndOrdersOutsideAreaDropsOrders()
	{
		const string solverDataJson =
		"""
          {
            "OrderPriorityType" : [ 1, 0, 1, 1 ],
            "NumberOfRoutes" : 1,
            "TimeMatrix" : [ [ 0, 1559, 2388, 2422 ], [ 1801, 0, 2342, 2336 ], [ 2636, 2312, 0, 120 ], [ 2596, 2271, 53, 0 ] ],
            "DistanceMatrix" : [ [ 0, 33600, 54410, 54705 ], [ 36577, 0, 50926, 39547 ], [ 57566, 50807, 0, 884 ], [ 57272, 50513, 434, 0 ] ],
            "StartLocations" : [ 0 ],
            "EndLocations" : [ 1 ],
            "PickupDropOffs" : [ [ 2, 3 ] ],
            "TimeWindows" : [ [ 405660, 424402 ], [ 405660, 477540 ], [ 432660, 435300 ], [ 431760, 435300 ] ],
            "CapacityDemands" : {
              "0" : [ 0, 0, 1, -1 ]
            },
            "VehicleCapacities" : {
              "0" : [ 4 ]
            },
            "VehicleTimeCapacities" : [ 477540 ],
            "Locations" : [ {
              "LocationId" : "StartDepot-580039",
              "Sequence" : null,
              "Coordinate" : {
                "Latitude" : 35.4992,
                "Longitude" : -86.8578
              },
              "StartTime" : "2024-11-12T15:13:21.753Z",
              "EndTime" : "2024-11-12T15:13:22.753Z",
              "LoadingTime" : "00:00:00",
              "UnloadingTime" : null
            }, {
              "LocationId" : "EndDepot-580039",
              "Sequence" : null,
              "Coordinate" : {
                "Latitude" : 35.6378,
                "Longitude" : -87.0411
              },
              "StartTime" : "2024-11-12T10:01:00Z",
              "EndTime" : "2024-11-13T05:59:00Z",
              "LoadingTime" : "00:00:00",
              "UnloadingTime" : "00:00:00"
            }, {
              "LocationId" : "P-1234",
              "Sequence" : 0,
              "Coordinate" : {
                "Latitude" : 35.926635978831335,
                "Longitude" : -86.86779209999789
              },
              "StartTime" : "2024-11-12T17:30:00Z",
              "EndTime" : "2024-11-12T18:15:00Z",
              "LoadingTime" : "00:01:00",
              "UnloadingTime" : null
            }, {
              "LocationId" : "D-1234",
              "Sequence" : 0,
              "Coordinate" : {
                "Latitude" : 35.92488535195896,
                "Longitude" : -86.87058696179064
              },
              "StartTime" : "2024-11-12T17:15:00Z",
              "EndTime" : "2024-11-12T18:15:00Z",
              "LoadingTime" : "00:01:00",
              "UnloadingTime" : null
            } ],
            "RouteMaxDurations" : [ 2147483647 ],
            "ArcCostMatrix" : [ [ [ 0, 1559, 2388, 2422 ], [ 1801, 0, 2342, 2336 ], [ 2636, 2312, 0, 120 ], [ 2596, 2271, 53, 0 ] ] ]
          }
        """;

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		options.Converters.Add(new GeoJsonConverterFactory());

		var requestDataJsonString = await File.ReadAllTextAsync("../../../TestData/RouteZoneOutOfBoundsRequestData.json");
		var requestData = JsonSerializer.Deserialize<VrpSolverRequest>(requestDataJsonString, options);
		var solverData = JsonConvert.DeserializeObject<OrToolsSolverData>(solverDataJson);
		var solverResponseJsonString = await File.ReadAllTextAsync("../../../TestData/RouteZoneOutOfBoundsResult.json");
		var solverResponse = JsonConvert.DeserializeObject<VrpSolverResponse>(solverResponseJsonString);

		Assert.NotNull(solverData);
		Assert.NotNull(requestData);
		Assert.NotNull(solverResponse);

		var timeConverter = new TimeConverter();
		// Convert a DateTime to seconds to set the TimeConverter's quotient
		timeConverter.ConvertToSeconds(requestData.Routes.First().StartDepot.StartTime);

		var solver = new Solver();
		var response = await solver.SolveRoutes(requestData, solverData, timeConverter);

		Assert.NotNull(response);
		Assert.Equal(VrpBugDemo.Enums.SolveStatus.Success, response.Status);
		Assert.Equal(2, response.UnassignableOrders.Count);
		Assert.Equivalent(solverResponse.UnassignableOrders, response.UnassignableOrders);
		Assert.Equivalent(solverResponse.Routes, response.Routes);
	}

	[Fact]
	public async Task AllOrdersAreAssignedAndTimesAreCorrect()
	{
		var solverDataJson =
		"""
		{
		  "OrderPriorityType" : [ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 ],
		  "NumberOfRoutes" : 1,
		  "TimeMatrix" : [ [ 0, 68, 216, 389, 264, 398, 216, 299, 4, 309 ], [ 77, 0, 226, 377, 243, 412, 226, 314, 82, 319 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 390, 348, 256, 0, 206, 452, 256, 427, 394, 170 ], [ 325, 275, 236, 271, 0, 478, 236, 384, 329, 288 ], [ 468, 448, 329, 505, 473, 0, 329, 248, 473, 477 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 400, 380, 269, 515, 413, 285, 269, 0, 405, 417 ], [ 54, 64, 212, 385, 259, 393, 212, 295, 0, 305 ], [ 374, 332, 204, 225, 288, 488, 204, 394, 379, 0 ] ],
		  "DistanceMatrix" : [ [ 0, 360, 1725, 3509, 2043, 3764, 1725, 2635, 17, 2591 ], [ 421, 0, 1550, 3217, 1680, 3964, 1550, 2835, 439, 2416 ], [ 1879, 1625, 0, 2322, 1397, 2104, 0, 1592, 1897, 1355 ], [ 3556, 3303, 2223, 0, 1531, 4794, 2223, 3576, 3574, 1187 ], [ 2112, 1762, 1312, 1552, 0, 2965, 1312, 2360, 2130, 2171 ], [ 3767, 3733, 2017, 4664, 2959, 0, 2017, 1725, 3785, 3130 ], [ 1879, 1625, 0, 2322, 1397, 2104, 0, 1592, 1897, 1355 ], [ 3124, 3090, 1735, 3915, 2678, 2211, 1735, 0, 3142, 2848 ], [ 199, 342, 1707, 3491, 2025, 3746, 1707, 2617, 0, 2573 ], [ 2675, 2421, 1293, 1177, 2186, 3130, 1293, 2525, 2693, 0 ] ],
		  "StartLocations" : [ 0 ],
		  "EndLocations" : [ 1 ],
		  "PickupDropOffs" : [ [ 2, 3 ], [ 4, 5 ], [ 6, 7 ], [ 8, 9 ] ],
		  "TimeWindows" : [ [ 808000, 837750 ], [ 808000, 844060 ], [ 841390, 843100 ], [ 841747, 844060 ], [ 840490, 842200 ], [ 840846, 843456 ], [ 839290, 841000 ], [ 839550, 842160 ], [ 838690, 840400 ], [ 839040, 841650 ] ],
		  "CapacityDemands" : {
		    "0" : [ 0, 0, 1, -1, 1, -1, 1, -1, 1, -1 ]
		  },
		  "VehicleCapacities" : {
		    "0" : [ 12 ]
		  },
		  "VehicleTimeCapacities" : [ 844060 ],
		  "Locations" : [ {
		    "LocationId" : "StartDepot-590493",
		    "Sequence" : null,
		    "Coordinate" : {
		      "Latitude" : 39.4122,
		      "Longitude" : -81.4376
		    },
		    "StartTime" : "2024-11-05T20:15:49.42Z",
		    "EndTime" : "2024-11-05T20:15:50.42Z",
		    "LoadingTime" : "00:00:00",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "EndDepot-590493",
		    "Sequence" : null,
		    "Coordinate" : {
		      "Latitude" : 39.4112,
		      "Longitude" : -81.4404
		    },
		    "StartTime" : "2024-11-05T12:00:00Z",
		    "EndTime" : "2024-11-05T22:01:00Z",
		    "LoadingTime" : "00:00:00",
		    "UnloadingTime" : "00:00:00"
		  }, {
		    "LocationId" : "P-42993",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4222,
		      "Longitude" : -81.4477
		    },
		    "StartTime" : "2024-11-05T21:15:00Z",
		    "EndTime" : "2024-11-05T21:45:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-42993",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4235,
		      "Longitude" : -81.4654
		    },
		    "StartTime" : "2024-11-05T21:20:57Z",
		    "EndTime" : "2024-11-05T22:05:57Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43094",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.414,
		      "Longitude" : -81.4551
		    },
		    "StartTime" : "2024-11-05T21:00:00Z",
		    "EndTime" : "2024-11-05T21:30:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43094",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4332,
		      "Longitude" : -81.4411
		    },
		    "StartTime" : "2024-11-05T21:05:56Z",
		    "EndTime" : "2024-11-05T21:50:56Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43134",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4222,
		      "Longitude" : -81.4477
		    },
		    "StartTime" : "2024-11-05T20:40:00Z",
		    "EndTime" : "2024-11-05T21:10:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43134",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4268,
		      "Longitude" : -81.4388
		    },
		    "StartTime" : "2024-11-05T20:44:20Z",
		    "EndTime" : "2024-11-05T21:29:20Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43136",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4124,
		      "Longitude" : -81.4376
		    },
		    "StartTime" : "2024-11-05T20:30:00Z",
		    "EndTime" : "2024-11-05T21:00:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43136",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4279,
		      "Longitude" : -81.4562
		    },
		    "StartTime" : "2024-11-05T20:35:50Z",
		    "EndTime" : "2024-11-05T21:20:50Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  } ],
		  "RouteMaxDurations" : [ 2147483647 ],
		  "ArcCostMatrix" : [ [ [ 0, 68, 216, 389, 264, 398, 216, 299, 4, 309 ], [ 77, 0, 226, 377, 243, 412, 226, 314, 82, 319 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 390, 348, 256, 0, 206, 452, 256, 427, 394, 170 ], [ 325, 275, 236, 271, 0, 478, 236, 384, 329, 288 ], [ 468, 448, 329, 505, 473, 0, 329, 248, 473, 477 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 400, 380, 269, 515, 413, 285, 269, 0, 405, 417 ], [ 54, 64, 212, 385, 259, 393, 212, 295, 0, 305 ], [ 374, 332, 204, 225, 288, 488, 204, 394, 379, 0 ] ] ]
		}
		""";

		var requestDataJsonString = await File.ReadAllTextAsync("../../../TestData/SmallSolveRequestData.json");
		var requestData = JsonConvert.DeserializeObject<VrpSolverRequest>(requestDataJsonString);
		var solverData = JsonConvert.DeserializeObject<OrToolsSolverData>(solverDataJson);
		var solverResponseJsonString = await File.ReadAllTextAsync("../../../TestData/SmallSolveResult.json");
		var solverResponse = JsonConvert.DeserializeObject<VrpSolverResponse>(solverResponseJsonString);

		Assert.NotNull(solverData);
		Assert.NotNull(requestData);
		Assert.NotNull(solverResponse);

		var timeConverter = new TimeConverter();
		// Convert a DateTime to seconds to set the TimeConverter's quotient
		timeConverter.ConvertToSeconds(requestData.Routes.First().StartDepot.StartTime);

		var solver = new Solver();
		var response = await solver.SolveRoutes(requestData, solverData, timeConverter);

		Assert.NotNull(response);
		Assert.Equal(VrpBugDemo.Enums.SolveStatus.Success, response.Status);
		Assert.Equivalent(solverResponse.UnassignableOrders, response.UnassignableOrders);
		Assert.Equivalent(solverResponse.Routes, response.Routes);

		var route = response.Routes.First();
		var firstOrder = route.Orders.First();
		var secondOrder = route.Orders[1];

		Assert.Equal(TimeSpan.FromMinutes(2), requestData.Routes[0].ArriveDepartDelay);
		Assert.Equal(TimeSpan.FromMinutes(1.5), firstOrder.Location.LoadingTime);
		Assert.Equal(firstOrder.Location.LoadingTime, firstOrder.Location.EndTime - firstOrder.Location.StartTime - firstOrder.WaitTime);
		Assert.Equal(firstOrder.Location.EndTime + secondOrder.TimeSinceLastOrder + requestData.Routes[0].ArriveDepartDelay, secondOrder.Location.StartTime);

		var correspondingRequestOrder = OrderSearch.FindOrder(requestData.Routes.First().Orders, order => order.OrderId == firstOrder.OrderId);

		Assert.True((firstOrder.Location.StartTime + firstOrder.WaitTime) >= correspondingRequestOrder.Location.StartTime.ToLocalTime());
		Assert.True(firstOrder.Location.StartTime < correspondingRequestOrder.Location.EndTime.ToLocalTime());
		Assert.True(firstOrder.Location.EndTime > correspondingRequestOrder.Location.StartTime.ToLocalTime());
		Assert.True(firstOrder.Location.EndTime <= correspondingRequestOrder.Location.EndTime.ToLocalTime());
	}

	[Fact]
	public async Task RouteWithMaxDurationOfFortyFiveMinutesLeavesFourOrdersUnassigned()
	{
		var solverDataJson =
		"""
		{
		  "OrderPriorityType" : [ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 ],
		  "NumberOfRoutes" : 1,
		  "TimeMatrix" : [ [ 0, 68, 216, 389, 264, 398, 216, 299, 4, 309 ], [ 77, 0, 226, 377, 243, 412, 226, 314, 82, 319 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 390, 348, 256, 0, 206, 452, 256, 427, 394, 170 ], [ 325, 275, 236, 271, 0, 478, 236, 384, 329, 288 ], [ 468, 448, 329, 505, 473, 0, 329, 248, 473, 477 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 400, 380, 269, 515, 413, 285, 269, 0, 405, 417 ], [ 54, 64, 212, 385, 259, 393, 212, 295, 0, 305 ], [ 374, 332, 204, 225, 288, 488, 204, 394, 379, 0 ] ],
		  "DistanceMatrix" : [ [ 0, 360, 1725, 3509, 2043, 3764, 1725, 2635, 17, 2591 ], [ 421, 0, 1550, 3217, 1680, 3964, 1550, 2835, 439, 2416 ], [ 1879, 1625, 0, 2322, 1397, 2104, 0, 1592, 1897, 1355 ], [ 3556, 3303, 2223, 0, 1531, 4794, 2223, 3576, 3574, 1187 ], [ 2112, 1762, 1312, 1552, 0, 2965, 1312, 2360, 2130, 2171 ], [ 3767, 3733, 2017, 4664, 2959, 0, 2017, 1725, 3785, 3130 ], [ 1879, 1625, 0, 2322, 1397, 2104, 0, 1592, 1897, 1355 ], [ 3124, 3090, 1735, 3915, 2678, 2211, 1735, 0, 3142, 2848 ], [ 199, 342, 1707, 3491, 2025, 3746, 1707, 2617, 0, 2573 ], [ 2675, 2421, 1293, 1177, 2186, 3130, 1293, 2525, 2693, 0 ] ],
		  "StartLocations" : [ 0 ],
		  "EndLocations" : [ 1 ],
		  "PickupDropOffs" : [ [ 2, 3 ], [ 4, 5 ], [ 6, 7 ], [ 8, 9 ] ],
		  "TimeWindows" : [ [ 808000, 837750 ], [ 808000, 844060 ], [ 841390, 843100 ], [ 841747, 844060 ], [ 840490, 842200 ], [ 840846, 843456 ], [ 839290, 841000 ], [ 839550, 842160 ], [ 838690, 840400 ], [ 839040, 841650 ] ],
		  "CapacityDemands" : {
		    "0" : [ 0, 0, 1, -1, 1, -1, 1, -1, 1, -1 ]
		  },
		  "VehicleCapacities" : {
		    "0" : [ 12 ]
		  },
		  "VehicleTimeCapacities" : [ 844060 ],
		  "Locations" : [ {
		    "LocationId" : "StartDepot-590493",
		    "Sequence" : null,
		    "Coordinate" : {
		      "Latitude" : 39.4122,
		      "Longitude" : -81.4376
		    },
		    "StartTime" : "2024-11-05T20:15:49.42Z",
		    "EndTime" : "2024-11-05T20:15:50.42Z",
		    "LoadingTime" : "00:00:00",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "EndDepot-590493",
		    "Sequence" : null,
		    "Coordinate" : {
		      "Latitude" : 39.4112,
		      "Longitude" : -81.4404
		    },
		    "StartTime" : "2024-11-05T12:00:00Z",
		    "EndTime" : "2024-11-05T22:01:00Z",
		    "LoadingTime" : "00:00:00",
		    "UnloadingTime" : "00:00:00"
		  }, {
		    "LocationId" : "P-42993",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4222,
		      "Longitude" : -81.4477
		    },
		    "StartTime" : "2024-11-05T21:15:00Z",
		    "EndTime" : "2024-11-05T21:45:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-42993",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4235,
		      "Longitude" : -81.4654
		    },
		    "StartTime" : "2024-11-05T21:20:57Z",
		    "EndTime" : "2024-11-05T22:05:57Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43094",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.414,
		      "Longitude" : -81.4551
		    },
		    "StartTime" : "2024-11-05T21:00:00Z",
		    "EndTime" : "2024-11-05T21:30:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43094",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4332,
		      "Longitude" : -81.4411
		    },
		    "StartTime" : "2024-11-05T21:05:56Z",
		    "EndTime" : "2024-11-05T21:50:56Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43134",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4222,
		      "Longitude" : -81.4477
		    },
		    "StartTime" : "2024-11-05T20:40:00Z",
		    "EndTime" : "2024-11-05T21:10:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43134",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4268,
		      "Longitude" : -81.4388
		    },
		    "StartTime" : "2024-11-05T20:44:20Z",
		    "EndTime" : "2024-11-05T21:29:20Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "P-43136",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4124,
		      "Longitude" : -81.4376
		    },
		    "StartTime" : "2024-11-05T20:30:00Z",
		    "EndTime" : "2024-11-05T21:00:00Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  }, {
		    "LocationId" : "D-43136",
		    "Sequence" : 0,
		    "Coordinate" : {
		      "Latitude" : 39.4279,
		      "Longitude" : -81.4562
		    },
		    "StartTime" : "2024-11-05T20:35:50Z",
		    "EndTime" : "2024-11-05T21:20:50Z",
		    "LoadingTime" : "00:01:30",
		    "UnloadingTime" : null
		  } ],
		  "RouteMaxDurations" : [ 2700 ],
		  "ArcCostMatrix" : [ [ [ 0, 68, 216, 389, 264, 398, 216, 299, 4, 309 ], [ 77, 0, 226, 377, 243, 412, 226, 314, 82, 319 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 390, 348, 256, 0, 206, 452, 256, 427, 394, 170 ], [ 325, 275, 236, 271, 0, 478, 236, 384, 329, 288 ], [ 468, 448, 329, 505, 473, 0, 329, 248, 473, 477 ], [ 285, 244, 0, 324, 247, 323, 0, 272, 290, 206 ], [ 400, 380, 269, 515, 413, 285, 269, 0, 405, 417 ], [ 54, 64, 212, 385, 259, 393, 212, 295, 0, 305 ], [ 374, 332, 204, 225, 288, 488, 204, 394, 379, 0 ] ] ]
		}
		""";

		var requestDataJsonString = await File.ReadAllTextAsync("../../../TestData/SmallSolveRequestData.json");
		var requestData = JsonConvert.DeserializeObject<VrpSolverRequest>(requestDataJsonString);
		var solverData = JsonConvert.DeserializeObject<OrToolsSolverData>(solverDataJson);

		Assert.NotNull(solverData);
		Assert.NotNull(requestData);

		var timeConverter = new TimeConverter();
		// Convert a DateTime to seconds to set the TimeConverter's quotient
		timeConverter.ConvertToSeconds(requestData.Routes.First().StartDepot.StartTime);

		var solver = new Solver();
		var response = await solver.SolveRoutes(requestData, solverData, timeConverter);

		Assert.NotNull(response);
		Assert.Equal(VrpBugDemo.Enums.SolveStatus.Success, response.Status);
		Assert.Equal(6, response.UnassignableOrders.Count);
		Assert.Equal(2, response.Routes.SelectMany(route => route.Orders).Count());
	}
}