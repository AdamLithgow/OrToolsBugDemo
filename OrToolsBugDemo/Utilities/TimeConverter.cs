namespace VrpBugDemo.Utilities;

public class TimeConverter
{
	private long _quotient;

	public DateTime ConvertToDateTime(long seconds)
	{
		var ms = (long)(Math.Pow(10, 9) * _quotient + (seconds * 1000));
		return DateTimeOffset.FromUnixTimeMilliseconds(ms).DateTime;
	}

	public long ConvertToSeconds(DateTime dateTime)
	{
		var ms = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

		if (_quotient != 0) return (long)(ms - (Math.Pow(10, 9) * _quotient)) / 1000;

		_quotient = Math.DivRem(ms, (long)Math.Pow(10, 9), out var msNoPrefix);
		return msNoPrefix / 1000;
	}
}