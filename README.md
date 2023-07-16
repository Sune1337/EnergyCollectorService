# HOWTO - Build and deploy docker-image
## Build docker image.
Remember to replace the version number with the current version.

docker build -t energycollectorservice:1.0.0 -f EnergyCollectorService/Dockerfile .

## Export docker image to file
Remember to replace the version number with the current version.

docker save energycollectorservice:1.0.0 | gzip > energycollectorservice-1.0.0.tar.gz

## Import docker image from file

gunzip --stdout energycollectorservice-1.0.0.tar.gz | docker load
