// <copyright file="URINameHelpers.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper functions for parsing the name URI.
    /// </summary>
    public static class URINameHelpers
    {
        /// <summary>
        /// Builds the scheme-less name for use when querying resources.
        /// </summary>
        /// <param name="bridgeSerial">The serial number of the bridge.</param>
        /// <param name="deviceSerial">The serial number of the device (default: null).</param>
        /// <returns>A URI formatted name.</returns>
        public static string BuildNameFromSerialNumbers(string bridgeSerial, string deviceSerial = null)
        {
            var name = $"//summit/bridge/{bridgeSerial.Trim()}";

            if (deviceSerial != null)
            {
                name = $"{name}/device/{deviceSerial.Trim()}";
            }

            return name;
        }

        /// <summary>
        /// Builds the scheme-less name for use when querying resources.
        /// </summary>
        /// <param name="bridgeName">The URI name of the bridge.</param>
        /// <param name="deviceSerial">The serial number of the device (default: null).</param>
        /// <returns>A URI formatted name.</returns>
        public static string BuildNameFromBridgeNameDeviceSerial(string bridgeName, string deviceSerial = null)
        {
            var name = bridgeName;

            if (deviceSerial != null)
            {
                name = $"{name}/device/{deviceSerial.Trim()}";
            }

            return name;
        }

        /// <summary>
        /// Parse the bridge and device name out of the name provided.
        /// </summary>
        /// <param name="fullName">The scheme-less URI representing an object.</param>
        /// <returns>A tuple containing the bridge name and the full device name.</returns>
        public static (string BridgeName, string FullName) ParseName(string fullName)
        {
            var bridgeName = Regex.Replace(fullName, @"/device/.*$", string.Empty);
            return (bridgeName, fullName);
        }

        /// <summary>
        /// Parse the name to get the bridge serial number.
        /// </summary>
        /// <param name="name">The full name of the resource.</param>
        /// <returns>The bridge serial number from the name URI.</returns>
        public static string GetBridgeSerial(string name)
        {
            var match = Regex.Match(name, @"/bridge/(\w*)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            throw new ArgumentException("No bridge serial found in name: {0}", name);
        }
    }
}
