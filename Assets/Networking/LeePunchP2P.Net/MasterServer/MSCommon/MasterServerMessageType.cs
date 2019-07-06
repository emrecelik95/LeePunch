
namespace LeePunchP2P.Net.MSCommon
{
	public enum MasterServerMessageType
	{
		RegisterHost = 1 << 0,
        RequestHost = 1 << 1,
        RequestHostList = 1 << 2,
		RequestNatHolePunch = 1 << 3,
        RequestExternalEP = 1 << 4
	}
}
