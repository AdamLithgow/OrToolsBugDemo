namespace VrpBugDemo.Enums;

public class SolveStatus
{
	private SolveStatus(string value) { Value = value; }

	public string Value { get; private set; }

	/// <summary>
	/// Problem not solved yet
	/// </summary>
	public static readonly SolveStatus NotSolved = new("ROUTING_NOT_SOLVED");
	/// <summary>
	/// Problem solved successfully
	/// </summary>
	public static readonly SolveStatus Success = new("ROUTING_SUCCESS");
	/// <summary>
	/// Problem solved successfully, except that a local optimum has not been reached.
	/// Leaving more time would allow improving the solution
	/// </summary>
	public static readonly SolveStatus PartialSuccess = new("ROUTING_PARTIAL_SUCCESS_LOCAL_OPTIMUM_NOT_REACHED");
	/// <summary>
	/// No solution found to the problem
	/// </summary>
	public static readonly SolveStatus Fail = new("ROUTING_FAIL");
	/// <summary>
	/// Time limit reached before finding a solution
	/// </summary>
	public static readonly SolveStatus Timeout = new("ROUTING_FAIL_TIMEOUT");
	/// <summary>
	/// Model, model parameters, or flags are not valid
	/// </summary>
	public static readonly SolveStatus Invalid = new("ROUTING_INVALID");
	/// <summary>
	/// Problem proven to be infeasible
	/// </summary>
	public static readonly SolveStatus Infeasible = new("ROUTING_INFEASIBLE");
	/// <summary>
	/// Problem has been solved to optimality
	/// </summary>
	public static readonly SolveStatus Optimal = new("ROUTING_OPTIMAL");
	public static readonly SolveStatus Unknown = new("UNKNOWN_STATUS");

	public override string ToString()
	{
		return Value;
	}

	public static SolveStatus GetSolveStatusMessage(int orToolsSolveStatus)
	{
		return orToolsSolveStatus switch
		{
			0 => NotSolved,
			1 => Success,
			2 => PartialSuccess,
			3 => Fail,
			4 => Timeout,
			5 => Invalid,
			6 => Infeasible,
			7 => Optimal,
			_ => Unknown
		};
	}
}