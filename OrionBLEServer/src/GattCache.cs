using InTheHand.Bluetooth;

namespace OrionBLEServer.src;

public record GattCache(Dictionary<GattService, List<GattCharacteristic>> Characteristics, BluetoothDevice device) {
	
	public static GattCache FromDevice(BluetoothDevice device) {
		var services = device.Gatt.GetPrimaryServicesAsync().Result;
		var cache = new Dictionary<GattService, List<GattCharacteristic>>();
		foreach (var service in services) {
			var characteristics = service.GetCharacteristicsAsync().Result;
			cache.Add(service, characteristics.ToList());
		}
		return new GattCache(cache, device);
	}
	
	public List<GattService> Services => Characteristics.Keys.ToList();
	
	public GattService? GetService(Guid uuid) {
		return Services.FirstOrDefault(service => service.Uuid.Value == uuid);
	}
	
	public List<GattCharacteristic> GetCharacteristics(GattService service) {
		return Characteristics[service];
	}
	
	public GattCharacteristic? GetCharacteristic(GattService service, Guid uuid) {
		return Characteristics[service].FirstOrDefault(characteristic => characteristic.Uuid.Value == uuid);
	}
	
	public GattCharacteristic? GetCharacteristic(Guid serviceUuid, Guid characteristicUuid) {
		var service = Services.FirstOrDefault(service => service.Uuid.Value == serviceUuid);
		return GetCharacteristic(service, characteristicUuid);
	}
	
	public BluetoothDevice Device => device;
}