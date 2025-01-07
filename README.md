# OrionBLE Server

## The backend to the Java library that enables communication with Bluetooth Low Energy devices.

![](banner.png)

This is the core of the [OrionBLE](https://github.com/AnonymousHacker1279/OrionBLE) Java library. It exposes Bluetooth
services over a REST API, allowing interaction with BLE devices from any programming language. It is particularly
useful for languages without easy access to system radios, such as Java.

## Using in Custom Projects (outside OrionBLE)

If you wish to use this server in your own project, you'll need to build it from source and create an executable for
your platform. Currently, it only supports Windows, but Linux support is planned for the future.

REST API routes can easily be discovered by looking at the source code, specifically `OrionBLEServer.cs` in the root of 
the project.

## BLE Feature Support

Support is added as needed. If you need a feature that is not currently supported, feel free to open an issue or
contribute to the project.

### GATT Characteristics

| Property                   | Supported |
|----------------------------|-----------|
| Broadcast                  | No        |
| Read                       | Yes       |
| Write with No Response     | Yes       |
| Write                      | No        |
| Notify                     | Yes       |
| Indicate                   | No        |
| Authenticated Signed Write | No        |


## License

OrionBLE Server is MIT licensed. See the [LICENSE](LICENSE) file for more information.