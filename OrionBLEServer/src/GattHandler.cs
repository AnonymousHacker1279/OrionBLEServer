using InTheHand.Bluetooth;
using Microsoft.AspNetCore.Mvc;

namespace OrionBLEServer.src;

public class GattHandler {
	private static readonly Dictionary<string, GattCache> DeviceCache = new();

	/**
	 * Check if a device is connected.
	 */
	public static IResult CheckDeviceConnection(string deviceAddress) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			return Results.Json(new Dictionary<string, string> {
				{ "Connected", cachedDevice.Device.Gatt.IsConnected.ToString() }
			});
		}

		DiscoverGattServices(deviceAddress);
		
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice2)) {
			return Results.Json(new Dictionary<string, string> {
				{ "Connected", cachedDevice2.Device.Gatt.IsConnected.ToString() }
			});
		}
		
		return Results.BadRequest("Device not found");
	}

	/**
	 * Discover GATT services for a given device.
	 */
	public static IResult DiscoverGattServices(string deviceAddress) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice))
			return Results.Json(CreateJson(cachedDevice.Services));

		var device = BluetoothDevice.FromIdAsync(deviceAddress).Result;
		
		if (device == null) return Results.BadRequest("Device not found");
		
		DeviceCache.Add(deviceAddress, GattCache.FromDevice(device));

		return Results.Json(CreateJson(device.Gatt.GetPrimaryServicesAsync().Result));
	}

	/**
	 * Discover GATT characteristics for a given service.
	 */
	public static IResult DiscoverGattCharacteristics(string deviceAddress, string serviceUuid) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristics = cachedDevice.GetCharacteristics(service);
			return Results.Json(CreateJson(characteristics));
		}

		DiscoverGattServices(deviceAddress);
		return DiscoverGattCharacteristics(deviceAddress, serviceUuid);
	}

	/**
	 * Read a message from the specified device.
	 */
	public static IResult ReadMessage(string deviceAddress, string serviceUuid, string characteristicUuid) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristic = cachedDevice.GetCharacteristic(service, new Guid(characteristicUuid));

			if (characteristic == null) return Results.BadRequest("Characteristic not found");

			var message = characteristic.ReadValueAsync().Result;
			return Results.Json(new List<byte>(message));
		}

		DiscoverGattServices(deviceAddress);
		return ReadMessage(deviceAddress, serviceUuid, characteristicUuid);
	}

	/**
	 * Send a message to the specified device.
	 */
	public static IResult SendMessage(string deviceAddress, string serviceUuid, string characteristicUuid,
		List<int> message) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristic = cachedDevice.GetCharacteristic(service, new Guid(characteristicUuid));

			if (characteristic == null) return Results.BadRequest("Characteristic not found");

			var messageByteArray = new byte[message.Count];
			for (var i = 0; i < message.Count; i++) messageByteArray[i] = (byte)message[i];
			characteristic.WriteValueWithoutResponseAsync(messageByteArray);
			return Results.Ok();
		}

		DiscoverGattServices(deviceAddress);
		return SendMessage(deviceAddress, serviceUuid, characteristicUuid, message);
	}

	public static IResult RegisterNotify(string deviceAddress, string serviceUuid, string characteristicUuid) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristic = cachedDevice.GetCharacteristic(service, new Guid(characteristicUuid));

			if (characteristic == null) return Results.BadRequest("Characteristic not found");

			if (!characteristic.Properties.HasFlag(GattCharacteristicProperties.Notify) &&
			    !characteristic.Properties.HasFlag(GattCharacteristicProperties.Indicate))
				return Results.BadRequest("Characteristic does not support notifications or indications");

			// Check if notifications are already enabled
			if (!BleNotifyHandler.HasDevice(deviceAddress)) {
				BleNotifyHandler.AddDevice(deviceAddress);
				characteristic.CharacteristicValueChanged += BleNotifyHandler.CharacteristicValueChanged;
				characteristic.StartNotificationsAsync();
			}

			return Results.Ok();
		}

		DiscoverGattServices(deviceAddress);
		return RegisterNotify(deviceAddress, serviceUuid, characteristicUuid);
	}

	public static IResult UnregisterNotify(string deviceAddress, string serviceUuid, string characteristicUuid) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristic = cachedDevice.GetCharacteristic(service, new Guid(characteristicUuid));

			if (characteristic == null) return Results.BadRequest("Characteristic not found");

			if (!characteristic.Properties.HasFlag(GattCharacteristicProperties.Notify) &&
			    !characteristic.Properties.HasFlag(GattCharacteristicProperties.Indicate))
				return Results.BadRequest("Characteristic does not support notifications or indications");

			// Check if notifications are already enabled
			if (BleNotifyHandler.HasDevice(deviceAddress)) {
				BleNotifyHandler.RemoveDevice(deviceAddress);
				characteristic.CharacteristicValueChanged -= BleNotifyHandler.CharacteristicValueChanged;
			}

			return Results.Ok();
		}

		DiscoverGattServices(deviceAddress);
		return UnregisterNotify(deviceAddress, serviceUuid, characteristicUuid);
	}

	public static IResult GetNotifications(string deviceAddress, string serviceUuid, string characteristicUuid) {
		if (DeviceCache.TryGetValue(deviceAddress, out var cachedDevice)) {
			var service = cachedDevice.GetService(new Guid(serviceUuid));

			if (service == null) return Results.BadRequest("Service not found");

			var characteristic = cachedDevice.GetCharacteristic(service, new Guid(characteristicUuid));

			if (characteristic == null) return Results.BadRequest("Characteristic not found");

			return BleNotifyHandler.GetNotifications(deviceAddress, service, characteristic);
		}

		DiscoverGattServices(deviceAddress);
		return GetNotifications(deviceAddress, serviceUuid, characteristicUuid);
	}

	private static List<Dictionary<string, string>> CreateJson(List<GattService> services) {
		var json = new List<Dictionary<string, string>>();
		foreach (var service in services)
			json.Add(new Dictionary<string, string> {
				{ "Uuid", service.Uuid.Value.ToString() },
				{ "IsPrimary", service.IsPrimary.ToString() }
			});

		return json;
	}

	private static List<Dictionary<string, string>> CreateJson(IReadOnlyList<GattCharacteristic> characteristics) {
		var json = new List<Dictionary<string, string>>();
		foreach (var characteristic in characteristics)
			json.Add(new Dictionary<string, string> {
				{ "Uuid", characteristic.Uuid.Value.ToString() },
				{ "Description", characteristic.UserDescription },
				{ "Properties", characteristic.Properties.ToString() }
			});

		return json;
	}
}