﻿// COPYRIGHT 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using ORTS.Common.Native;

namespace ORTS.Common
{
    /// <summary>
    /// Base class for all means of persisting settings from the user/game.
    /// </summary>
    public abstract class SettingsStore
	{
        /// <summary>Name of a 'section', to distinguish various part within a underlying store</summary>
        protected string Section { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="section">Name of the 'section', to distinguish various part within a underlying store</param>
		protected SettingsStore(string section)
		{
			Section = section;
		}

        /// <summary>
        /// Assert that the type expected from the settings store is an allowed type.
        /// </summary>
        /// <param name="expectedType">Type that is expected</param>
        protected static void AssertGetUserValueType(Type expectedType)
        {
            Debug.Assert(new[] {
                typeof(bool),
                typeof(int),
                typeof(DateTime),
                typeof(TimeSpan),
                typeof(string),
                typeof(int[]),
                typeof(string[]),
                typeof(byte),
            }.Contains(expectedType), String.Format("GetUserValue called with unexpected type {0}.", expectedType.FullName));
        }

        /// <summary>
        /// Return an array of all setting-names that are in the store
        /// </summary>
        public abstract string[] GetUserNames();

        /// <summary>
        /// Get the value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="expectedType">Type that is expected</param>
        /// <returns>the value from the store, as a general object</returns>
        public abstract object GetUserValue(string name, Type expectedType);

        /// <summary>
        /// Set a boolean user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, bool value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, int value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, byte value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, DateTime value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, TimeSpan value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, string value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, int[] value);

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public abstract void SetUserValue(string name, string[] value);

        /// <summary>
        /// Remove a user setting from the store
        /// </summary>
        /// <param name="name">name of the setting</param>
		public abstract void DeleteUserValue(string name);

        /// <summary>
        /// Factory method to create a setting store (sub-class of SettingsStore)
        /// </summary>
        /// <param name="filePath">File patht o a .init file, if you want to use a .ini file</param>
        /// <param name="registryKey">key to the 'windows' register, if you want to use a registry-based store</param>
        /// <param name="section">Name to distinguish between various 'section's used in underlying store.</param>
        /// <returns>The created SettingsStore</returns>
		public static SettingsStore GetSettingStore(string filePath, string registryKey, string section)
		{
			if (!String.IsNullOrEmpty(filePath) && File.Exists(filePath))
				return new SettingsStoreLocalIni(filePath, section);
			if (!String.IsNullOrEmpty(registryKey))
				return new SettingsStoreRegistry(registryKey, section);
			throw new ArgumentException("Neither 'filePath' nor 'registryKey' arguments are valid.");
		}
	}

	/// <summary>
    /// Registry implementation of <see cref="SettingsStore"/>.
	/// </summary>
	public sealed class SettingsStoreRegistry : SettingsStore
	{
		readonly string RegistryKey;
		readonly RegistryKey Key;

		internal SettingsStoreRegistry(string registryKey, string section)
			: base(section)
		{
			RegistryKey = String.IsNullOrEmpty(section) ? registryKey : registryKey + @"\" + section;
			Key = Registry.CurrentUser.CreateSubKey(RegistryKey);
		}

        /// <summary>
        /// Return an array of all setting-names that are in the store
        /// </summary>
        public override string[] GetUserNames()
        {
            return Key.GetValueNames();
        }

        /// <summary>
        /// Get the value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="expectedType">Type that is expected</param>
        /// <returns>the value from the store, as a general object</returns>
        public override object GetUserValue(string name, Type expectedType)
        {
            AssertGetUserValueType(expectedType);

            var userValue = Key.GetValue(name);
            if (userValue == null)
                return userValue;

            try
            {
                // Expected bool-stored-as-int conversion.
                if (expectedType == typeof(bool) && (userValue is int))
                    return (int)userValue == 1;

                // Expected DateTime-stored-as-long conversion.
                if (expectedType == typeof(DateTime) && (userValue is long))
                    return DateTime.FromBinary((long)userValue);

                // Expected TimeSpan-stored-as-long conversion.
                if (expectedType == typeof(TimeSpan) && (userValue is long))
                    return TimeSpan.FromSeconds((long)userValue);

                // Expected int[]-stored-as-string conversion.
                if (expectedType == typeof(int[]) && (userValue is string))
                    return ((string)userValue).Split(',').Select(s => int.Parse(s)).ToArray();

                // Convert whatever we're left with into the expected type.
                return Convert.ChangeType(userValue, expectedType);
            }
            catch
            {
                Trace.TraceWarning("Setting {0} contains invalid value {1}.", name, userValue);
                return null;
            }
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, bool value)
        {
            Key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, int value)
        {
            Key.SetValue(name, value, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, byte value)
        {
            Key.SetValue(name, value, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, DateTime value)
        {
            Key.SetValue(name, value.ToBinary(), RegistryValueKind.QWord);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, TimeSpan value)
        {
            Key.SetValue(name, value.TotalSeconds, RegistryValueKind.QWord);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, string value)
        {
            Key.SetValue(name, value, RegistryValueKind.String);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, int[] value)
		{
			Key.SetValue(name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
		}

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, string[] value)
        {
            Key.SetValue(name, value, RegistryValueKind.MultiString);
        }

        /// <summary>
        /// Remove a user setting from the store
        /// </summary>
        /// <param name="name">name of the setting</param>
        public override void DeleteUserValue(string name)
		{
			Key.DeleteValue(name, false);
		}
	}

	/// <summary>
    /// INI file implementation of <see cref="SettingsStore"/>.
	/// </summary>
	public sealed class SettingsStoreLocalIni : SettingsStore
	{
		const string DefaultSection = "ORTS";

		readonly string FilePath;

		internal SettingsStoreLocalIni(string filePath, string section)
			: base(String.IsNullOrEmpty(section) ? DefaultSection : section)
		{
			FilePath = filePath;
		}

        /// <summary>
        /// Returns an array of all sections within the store, including the one used by this instance.
        /// </summary>
        /// <returns></returns>
        public string[] GetSectionNames()
        {
            var buffer = new String('\0', 256);
            while (true)
            {
                var length = NativeMethods.GetPrivateProfileString(null, null, null, buffer, buffer.Length, FilePath);
                if (length < buffer.Length - 2)
                {
                    buffer = buffer.Substring(0, length);
                    break;
                }
                buffer = new String('\0', buffer.Length * 2);
            }
            if (buffer.Length == 0)
                return null;

            return buffer.Split('\0');
        }

        /// <summary>
        /// Return an array of all setting-names that are in the store
        /// </summary>
        public override string[] GetUserNames()
        {
            var buffer = new String('\0', 256);
            while (true)
            {
                var length = NativeMethods.GetPrivateProfileSection(Section, buffer, buffer.Length, FilePath);
                if (length < buffer.Length - 2)
                {
                    buffer = buffer.Substring(0, length);
                    break;
                }
                buffer = new String('\0', buffer.Length * 2);
            }
            return buffer.Split('\0').Where(s => s.Contains('=')).Select(s => s.Split('=')[0]).ToArray();
        }

        /// <summary>
        /// Get the value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="expectedType">Type that is expected</param>
        /// <returns>the value from the store, as a general object</returns>
        public override object GetUserValue(string name, Type expectedType)
        {
            AssertGetUserValueType(expectedType);

            var buffer = new String('\0', 256);
            while (true)
            {
                var length = NativeMethods.GetPrivateProfileString(Section, name, null, buffer, buffer.Length, FilePath);
                if (length < buffer.Length - 1)
                {
                    buffer = buffer.Substring(0, length);
                    break;
                }
                buffer = new String('\0', buffer.Length * 2);
            }
            if (buffer.Length == 0)
                return null;

            var value = buffer.Split(':');
            if (value.Length != 2)
            {
                Trace.TraceWarning("Setting {0} contains invalid value {1}.", name, buffer);
                return null;
            }

            try
            {
                object userValue = null;
                switch (value[0])
                {
                    case "bool":
                        userValue = value[1].Equals("true", StringComparison.InvariantCultureIgnoreCase);
                        break;
                    case "int":
                        userValue = int.Parse(Uri.UnescapeDataString(value[1]), CultureInfo.InvariantCulture);
                        break;
                    case "byte":
                        userValue = byte.Parse(Uri.UnescapeDataString(value[1]), CultureInfo.InvariantCulture);
                        break;
                    case "DateTime":
                        userValue = DateTime.FromBinary(long.Parse(Uri.UnescapeDataString(value[1]), CultureInfo.InvariantCulture));
                        break;
                    case "TimeSpan":
                        userValue = TimeSpan.FromSeconds(long.Parse(Uri.UnescapeDataString(value[1]), CultureInfo.InvariantCulture));
                        break;
                    case "string":
                        userValue = Uri.UnescapeDataString(value[1]);
                        break;
                    case "int[]":
                        userValue = value[1].Split(',').Select(v => int.Parse(Uri.UnescapeDataString(v), CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case "string[]":
                        userValue = value[1].Split(',').Select(v => Uri.UnescapeDataString(v)).ToArray();
                        break;
                    default:
                        Trace.TraceWarning("Setting {0} contains invalid type {1}.", name, value[0]);
                        break;
                }

                // Convert whatever we're left with into the expected type.
                return Convert.ChangeType(userValue, expectedType);
            }
            catch
            {
                Trace.TraceWarning("Setting {0} contains invalid value {1}.", name, value[1]);
                return null;
            }
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, bool value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "bool:" + (value ? "true" : "false"), FilePath);
		}

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, int value)
        {
            NativeMethods.WritePrivateProfileString(Section, name, "int:" + Uri.EscapeDataString(value.ToString(CultureInfo.InvariantCulture)), FilePath);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, byte value)
        {
            NativeMethods.WritePrivateProfileString(Section, name, "byte:" + Uri.EscapeDataString(value.ToString(CultureInfo.InvariantCulture)), FilePath);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, DateTime value)
        {
            NativeMethods.WritePrivateProfileString(Section, name, "DateTime:" + Uri.EscapeDataString(value.ToBinary().ToString(CultureInfo.InvariantCulture)), FilePath);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, TimeSpan value)
        {
            NativeMethods.WritePrivateProfileString(Section, name, "TimeSpan:" + Uri.EscapeDataString(value.TotalSeconds.ToString(CultureInfo.InvariantCulture)), FilePath);
        }

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, string value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "string:" + Uri.EscapeDataString(value), FilePath);
		}

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, int[] value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "int[]:" + String.Join(",", ((int[])value).Select(v => Uri.EscapeDataString(v.ToString(CultureInfo.InvariantCulture))).ToArray()), FilePath);
		}

        /// <summary>
        /// Set a value of a user setting
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="value">value of the setting</param>
        public override void SetUserValue(string name, string[] value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "string[]:" + String.Join(",", value.Select(v => Uri.EscapeDataString(v)).ToArray()), FilePath);
		}

        /// <summary>
        /// Remove a user setting from the store
        /// </summary>
        /// <param name="name">name of the setting</param>
        public override void DeleteUserValue(string name)
		{
			NativeMethods.WritePrivateProfileString(Section, name, null, FilePath);
		}
	}
}
