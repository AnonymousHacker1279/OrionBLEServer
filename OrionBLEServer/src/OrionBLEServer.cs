using System.Text.Json;
using OrionBLEServer.src;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
	IncludeFields = true,
	WriteIndented = true
};

const string version = "1.0.0";

app.MapGet("/", () => Results.Text($"Orion BLE Server v{version}"))
	.WithName("Home");

app.MapGet("/devices/discover",
		(string? name, string? namePrefix) => DiscoveryHandler.HandleIncomingRequest(name, namePrefix))
	.WithName("DiscoverDevices");

app.MapGet("/devices/{address}",
		(string address) => GattHandler.CheckDeviceConnection(address))
	.WithName("CheckDeviceConnection");

app.MapGet("/devices/{address}/services",
		(string address) => GattHandler.DiscoverGattServices(address))
	.WithName("DiscoverDeviceServices");

app.MapGet("/devices/{address}/service/{service}",
		(string address, string service) => GattHandler.DiscoverGattCharacteristics(address, service))
	.WithName("DiscoverServiceCharacteristics");

app.MapGet("/devices/{address}/service/{service}/characteristic/{characteristic}/read",
		(string address, string service, string characteristic) =>
			GattHandler.ReadMessage(address, service, characteristic))
	.WithName("ReadMessage");

app.MapPost("/devices/{address}/service/{service}/characteristic/{characteristic}/write",
		(string address, string service, string characteristic, HttpContext context) => {
			// Get the message from the request body
			var message = context.Request.ReadFromJsonAsync<GenericJsonMessage>(options).Result;

			if (message == null) return Results.BadRequest("Invalid message");

			return GattHandler.SendMessage(address, service, characteristic, message.Message);
		})
	.WithName("WriteMessage");

app.MapPost("/devices/{address}/service/{service}/characteristic/{characteristic}/register_notify",
		(string address, string service, string characteristic) =>
			GattHandler.RegisterNotify(address, service, characteristic))
	.WithName("RegisterNotify");

app.MapPost("/devices/{address}/service/{service}/characteristic/{characteristic}/unregister_notify",
		(string address, string service, string characteristic) =>
			GattHandler.UnregisterNotify(address, service, characteristic))
	.WithName("UnregisterNotify");

app.MapGet("/devices/{address}/service/{service}/characteristic/{characteristic}/notifications",
		(string address, string service, string characteristic) =>
			GattHandler.GetNotifications(address, service, characteristic))
	.WithName("GetNotifications");

app.Run();

internal class GenericJsonMessage {
	public List<int> Message { get; set; }
}