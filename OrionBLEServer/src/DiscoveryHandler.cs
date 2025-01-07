using InTheHand.Bluetooth;

namespace OrionBLEServer.src;

public class DiscoveryHandler {

	public static IResult HandleIncomingRequest(string? name, string? namePrefix) {
		if (name == null && namePrefix == null) {
			return DiscoverDevices();
		}
		var options = new RequestDeviceOptions();
		if (name != null) {
			options.Filters.Add(new BluetoothLEScanFilter {
				Name = name
			});
		}
		if (namePrefix != null) {
			options.Filters.Add(new BluetoothLEScanFilter {
				NamePrefix = namePrefix
			});
		}
		
		return DiscoverDevices(options);
	}

	/**
	 * Discover nearby Bluetooth devices.
	 */
	private static IResult DiscoverDevices() {
		return Results.Json(CreateJson(Bluetooth.ScanForDevicesAsync().Result));
	}
	
	/**
	 * Discover nearby devices based on a given filter.
	 */
	private static IResult DiscoverDevices(RequestDeviceOptions options) {
		var devices = Bluetooth.ScanForDevicesAsync(options);
		return Results.Json(CreateJson(devices.Result));
	}

	private static List<Dictionary<string, string>> CreateJson(IReadOnlyCollection<BluetoothDevice> devices) {
		var json = new List<Dictionary<string, string>>();
		foreach (var device in devices) {
			json.Add(new Dictionary<string, string> {
				{ "Name", device.Name },
				{ "Address", device.Id },
				{ "Paired", device.IsPaired.ToString() }
			});
		}

		return json;
	}
}