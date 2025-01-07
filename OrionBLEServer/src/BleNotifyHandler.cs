using InTheHand.Bluetooth;

namespace OrionBLEServer.src;

public class BleNotifyHandler {
	private static readonly Dictionary<string, List<GattNotificationCache>> NotificationCache = new();

	public static void CharacteristicValueChanged(object sender, GattCharacteristicValueChangedEventArgs args) {
		if (sender is GattCharacteristic characteristic) {
			var cache = new GattNotificationCache(characteristic.Service, characteristic, args.Value);
			if (NotificationCache.TryGetValue(characteristic.Service.Device.Id, out var notifications))
				notifications.Add(cache);
			else
				NotificationCache.Add(characteristic.Service.Device.Id, new List<GattNotificationCache> { cache });
		}
	}

	public static IResult GetNotifications(string deviceAddress, GattService service,
		GattCharacteristic characteristic) {
		if (NotificationCache.TryGetValue(deviceAddress, out var cache)) {
			var notifications = cache.FindAll(notification =>
				notification.Service.Uuid.Value == service.Uuid.Value &&
				notification.Characteristic.Uuid.Value == characteristic.Uuid.Value);

			// Remove returned notifications from cache
			cache.RemoveAll(notification =>
				notification.Service.Uuid.Value == service.Uuid.Value &&
				notification.Characteristic.Uuid.Value == characteristic.Uuid.Value);

			if (notifications.Count > 0)
				return Results.Json(CreateJson(notifications));
		}

		return Results.BadRequest("No notifications found");
	}

	public static void AddDevice(string deviceAddress) {
		NotificationCache.Add(deviceAddress, new List<GattNotificationCache>());
	}

	public static bool HasDevice(string deviceAddress) {
		return NotificationCache.ContainsKey(deviceAddress);
	}

	public static void RemoveDevice(string deviceAddress) {
		NotificationCache.Remove(deviceAddress);
	}

	private static List<Dictionary<string, string>> CreateJson(List<GattNotificationCache> cache) {
		var json = new List<Dictionary<string, string>>();
		foreach (var notification in cache)
			json.Add(new Dictionary<string, string> {
				{ "Service", notification.Service.Uuid.Value.ToString() },
				{ "Characteristic", notification.Characteristic.Uuid.Value.ToString() },
				{ "Value", string.Join(" ", notification.Value) }
			});

		return json;
	}

	public record GattNotificationCache(GattService Service, GattCharacteristic Characteristic, byte[] Value);
}