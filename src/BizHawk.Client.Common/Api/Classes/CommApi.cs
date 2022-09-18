#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class CommApi : ICommApi
	{
		private static readonly WebSocketServer _wsServer = new WebSocketServer();

		private readonly (HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer? Sockets) _networkingHelpers;

		private readonly Config _config;

		public bool IsHttpAllowedInScripts() => _config.AllowHttpInLuaScripts;

		public HttpCommunication HTTP => _networkingHelpers.HTTP;

		public MemoryMappedFiles MMF => _networkingHelpers.MMF;

		public SocketServer? Sockets => _networkingHelpers.Sockets;

		public WebSocketServer WebSockets => _wsServer;

		public CommApi(IMainFormForApi mainForm, Config config)
		{
			_networkingHelpers = mainForm.NetworkingHelpers;
			_config = config;
		}

		public string? HttpTest() => IsHttpAllowedInScripts() ? string.Join("\n", HttpTestGet(), HTTP.SendScreenshot(), "done testing") : null;

		public string? HttpTestGet() => IsHttpAllowedInScripts() ? HTTP.Get(HTTP.GetUrl)?.Result : null;
	}
}
