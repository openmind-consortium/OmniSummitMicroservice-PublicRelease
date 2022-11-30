# OpenMindProtos

gRPC/protobuf source files for the OpenMind prototype.

## Overview

This folder contains the gRPC/protobuf source files for the OpenMind prototype.
The source files are broken into servcies with an additional `common.proto` file
for data structures that are common between services.

Both the client and server use these source files to autogenerate the client and
server code respectively. 