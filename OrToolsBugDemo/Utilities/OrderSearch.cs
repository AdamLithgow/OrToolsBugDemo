using VrpBugDemo.Models.Request;

namespace VrpBugDemo.Utilities;

public class OrderSearch
{
	public static OrderRequest? FindOrder(List<OrderRequest> orders, Func<OrderRequest, bool> comparisonFunction)
	{
		foreach (var order in orders)
		{
			var orderResult = CheckOrder(order, comparisonFunction);
			if (orderResult != null) return orderResult;
		}

		return null;
	}

	private static OrderRequest? CheckOrder(OrderRequest order, Func<OrderRequest, bool> comparisonFunction)
	{
		var doesOrderMatch = comparisonFunction(order);
		if (doesOrderMatch)
			return order;

		return order.SubsequentOrder == null
			? null
			: CheckOrder(order.SubsequentOrder, comparisonFunction);
	}
}