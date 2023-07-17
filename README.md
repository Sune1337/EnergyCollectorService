# HOWTO - Build and deploy docker-image
## Build docker image.
Remember to replace the version number with the current version.

docker build -t energycollectorservice:1.0.0 -f EnergyCollectorService/Dockerfile .

## Export docker image to file
Remember to replace the version number with the current version.

docker save energycollectorservice:1.0.0 | gzip > energycollectorservice-1.0.0.tar.gz

## Import docker image from file

gunzip --stdout energycollectorservice-1.0.0.tar.gz | docker load


## Generate .cs source from Entsoe Xml schema
1. Download xsd.zip from https://transparency.entsoe.eu/content/static_content/Static%20content/knowledge%20base/XSD.zip
2. Unzip it to .\EntsoeCollectorService\EntsoeApi\Models\xsd
3. in cmd.exe run:
```
cd .\EntsoeCollectorService\EntsoeApi\Models
xsd /namespace:EntsoeCollectorService.EntsoeApi.Models /c .\xsd\urn-entsoe-eu-local-extension-types.xsd .\xsd\urn-entsoe-eu-wgedi-codelists.xsd .\xsd\iec62325-451-6-generationload.xsd
```
