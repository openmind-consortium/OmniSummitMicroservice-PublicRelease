# OpenMindServer

OpenMind prototype server code.

## Overview

This project contains the server code written for the OpenMind prototype. The
server uses gRPC to define an API for client consumption. The server is written
in C# using .NET Framework, _not_ .NET Core. The gRPC implementation is based on
the native gRPC core library.

## Architecture

The OpenMind server prototype uses an `interface` driven design. The thought was
that if the OpenMind server project was to continue with this server as the base
code, `interfaces` could be leveraged to implement multiple different kinds of
neurostim devices.

The OpenMind server prototype also requires the use of caching mechanisms to make
sure that objects created by device SDKs live between client requests. 

The entrypoint for the source code is [Server.cs][Server.cs].

### Vocabulary

System refers to the kind of hardware the client is requesting access to. Right now
the available systems include the Summit RDK. Future systems will include Dyneumo.

Bridges provide a connection between the implantable neurostimulator (INS) and the PC.
Not all systems will have bridges, but many do.

Devices expose the INS functionalities to the client. These capabilities include
neurostim and sense, and may include additional functionality like accelerometer data.

Each different system has a Bridge and Device manager responsible for managing a cache
of bridge and device objects respectively. These caches are used to make sure the
connection to the bridge/device persists between client API calls.

Bridge and device managers offer connect and disconnect methods to help automate the
update of their internal caches.

### Interfaces

Interfaces are used instead of objects to keep the framework flexible. If we decide to
add additional hardware systems we can easily make bridge, device and manager objects
for that hardware system by implementing the interfaces provided.

The interfaces are defined alongside their implementations. I did not bother to create
extra files for interfaces as we only have one implementation for now.

## Installation

Follow the Summit RDK installation documentation to install the Summit prerequisites.

The Visual Studio solution should handle the rest of the installation.

## Running

To run, click the Start icon in Visual Studio.

## Future Work

Coming soon....