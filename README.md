# mqtt-graphite-bridge
A bridge to transfer data from an MQTT broker to a graphite instance. Written in C# and targeting .NET 5, it is aimed at running on 32 bit ARM devices (like a Raspberry Pie) under Linux as a systemd service, but should work on any other platform that is supported by .NET, too.

## Building
For the moment the recommended way to build/publish the project is a self contained, runtime specific single file package. The only runtime this has been tested for is `linux-arm`. The publish command looks like this:
> dotnet publish --runtime linux-arm --self-contained true --configuration Release /p:PublishSingleFile=true MqttGraphiteBridge

The necessary files to build a pacman package suitable for Arch Linux, Manjaro etc. are available from a [separate repo](https://github.com/bert-alpen/pkgbuild/tree/main/mqtt-graphite-bridge/). 

## Configuration
You can specifiy the configuration file in the environment variable `MQTT_GRAPHITE_BRIDGE_CONFIG_FILE`. If that variable isn't set the program attempts to laod the configuration from `appSettings.json` in the current directory. In either case, environment specific overrides can be specified. 

Examples (assuming that the environment for .NET has been set to `development`):
- If the environment variable points to the file `/etc/mqtt-graphite-bridge/mqtt-graphite-bridge.conf`, the configuration is read from that file. 
- `/etc/mqtt-graphite-bridge/mqtt-graphite-bridge.development.conf` is loaded if it is present

- If the environment is not set the configuration is read from `{current dir}\appSettings.json`
- `{current dir}\appSettings.development.json` is loaded if it is present


## Practical Tips
If you want to push your metrics to [Grafana Cloud](https://grafana.com/products/cloud/), configure an instance of [carbon-relay-ng](https://github.com/grafana/carbon-relay-ng) as a relay between this program and the carbon instance in Grafana cloud. If you have trouble compiling that project for armv7h, take a look at my [PKGBUILD](https://github.com/bert-alpen/pkgbuild/tree/main/carbon-relay-ng/) for it.
