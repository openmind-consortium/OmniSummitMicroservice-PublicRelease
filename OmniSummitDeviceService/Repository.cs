// <copyright file="Repository.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using System.Collections.Generic;
    using Medtronic.TelemetryM;
    using OpenMind;
    using OpenMindServer.Wrappers;

    /// <summary>
    /// Singleton container for caching CTM/INS connections and associated metadata.
    /// </summary>
    public class Repository
    {
        private static readonly Repository Instance = new Repository();

        /// <summary>
        /// Stores the address of the bridge, used to establish a connection.
        /// </summary>
        private readonly IDictionary<string, InstrumentInfo> cachedBridgeAddress;

        /// <summary>
        /// Stores the address of the device, used to establish a connection.
        /// </summary>
        private readonly IDictionary<string, DiscoveredDevice> cachedDeviceAddress;

        /// <summary>
        /// Stores active connections to the bridge.
        /// </summary>
        private readonly IDictionary<string, SummitServiceInfo> cachedConnection;

        private Repository()
        {
            this.cachedBridgeAddress = new Dictionary<string, InstrumentInfo>();
            this.cachedDeviceAddress = new Dictionary<string, DiscoveredDevice>();
            this.cachedConnection = new Dictionary<string, SummitServiceInfo>();
        }

        /// <summary>
        /// Gets the repository instance.
        /// </summary>
        /// <returns>The repository instance.</returns>
        public static Repository GetRepositoryInstance()
        {
            // TODO: Can we do this with getters/setters instead of a method?
            return Instance;
        }

        /// <summary>
        /// Gets active addresses in the repository.
        /// </summary>
        /// <returns>List of cached connections.</returns>
        public List<string> GetAddresses()
        {
            List<string> addresses = new List<string>();
            addresses.AddRange(this.cachedConnection.Keys);
            return addresses;
        }

        /// <summary>
        /// Stores a bridge address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        /// <param name="address">The address of the bridge.</param>
        public void CacheBridgeAddress(string name, InstrumentInfo address)
        {
            this.cachedBridgeAddress[name] = address;
        }

        /// <summary>
        /// Retrieves a bridge address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        /// <returns>The address of the bridge.</returns>
        public InstrumentInfo GetBridgeAddressByName(string name)
        {
            try
            {
                return this.cachedBridgeAddress[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Removes a bridge address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        public void RemoveBridgeAddressByName(string name)
        {
            if (this.cachedBridgeAddress.ContainsKey(name))
            {
                this.cachedBridgeAddress.Remove(name);
            }
        }

        /// <summary>
        /// Stores a device address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the device.</param>
        /// <param name="address">The address of the device.</param>
        public void CacheDeviceAddress(string name, DiscoveredDevice address)
        {
            this.cachedDeviceAddress[name] = address;
        }

        /// <summary>
        /// Retrieves a device address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the device.</param>
        /// <returns>The address of the device.</returns>
        public DiscoveredDevice? GetDeviceAddressByName(string name)
        {
            try
            {
                return this.cachedDeviceAddress[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Removes a device address by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the device.</param>
        public void RemoveDeviceAddressByName(string name)
        {
            if (this.cachedDeviceAddress.ContainsKey(name))
            {
                this.cachedDeviceAddress.Remove(name);
            }
        }

        /// <summary>
        /// Stores a connection by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        /// <param name="connection">The bridge connection.</param>
        /// <param name="theManager">The SummitManager object associated with this connection.</param>
        /// <param name="connectBridgeRequest">The original connection request.</param>
        /// <param name="address">The wireless address of the CTM connection.</param>
        public void CacheConnection(string name, ISummitSystem connection, ISummitManager theManager, ConnectBridgeRequest connectBridgeRequest, InstrumentInfo address)
        {
            this.cachedConnection[name] = new SummitServiceInfo(name, connection, theManager, connectBridgeRequest, address);
            this.cachedConnection[name].UnexpectedDisposalHandler += this.RepositoryItem_UnexpectedDisposalHandler;
        }

        /// <summary>
        /// Retrieves a connection by name.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        /// <returns>The bridge connection.</returns>
        public SummitServiceInfo GetConnectionByName(string name)
        {
            try
            {
                return this.cachedConnection[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Removes a connection from the cache.
        /// </summary>
        /// <param name="name">The scheme-less URI for the bridge.</param>
        public void RemoveConnectionByName(string name)
        {
            this.cachedConnection[name].Dispose();
            this.cachedConnection.Remove(name);
        }

        /// <summary>
        /// Remove all keys and values from all repository data structures.
        /// </summary>
        public void Clear()
        {
            // If a clear is being called, first dispose all services.
            foreach (KeyValuePair<string, SummitServiceInfo> aService in this.cachedConnection)
            {
                aService.Value.Dispose();
            }

            this.cachedBridgeAddress.Clear();
            this.cachedDeviceAddress.Clear();
            this.cachedConnection.Clear();
        }

        /// <summary>
        /// Removes an automatically-disposed SummitServiceInfo from the repository.
        /// </summary>
        /// <param name="sender">The SummitServiceInfo object that disposed itself.</param>
        /// <param name="e">Event arguments, presumed empty.</param>
        private void RepositoryItem_UnexpectedDisposalHandler(object sender, EventArgs e)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(((SummitServiceInfo)sender).Name);
            this.RemoveConnectionByName(bridgeName);
        }
    }
}
