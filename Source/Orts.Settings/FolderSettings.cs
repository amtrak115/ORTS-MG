﻿// COPYRIGHT 2014 by the Open Rails project.
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

using Orts.Settings.Store;

namespace Orts.Settings
{
    public class FolderSettings : SettingsBase
    {
        public Dictionary<string, string> Folders { get; } = new Dictionary<string, string>();

        public FolderSettings(in ImmutableArray<string> options, SettingsStore store) :
            base(SettingsStore.GetSettingsStore(store?.StoreType ?? StoreType.Json, store.Location, "Folders"))
        {
            LoadSettings(options);
        }

        public override object GetDefaultValue(string name)
        {
            return string.Empty;
        }

        protected override object GetValue(string name)
        {
            return Folders[name];
        }

        protected override void SetValue(string name, object value)
        {
            if (!string.IsNullOrWhiteSpace(value?.ToString()))
                Folders[name] = (string)value;
            else if (name != null)
                Folders.Remove(name);
        }

        protected override void Load(bool allowUserSettings, NameValueCollection optionalValues)
        {
            foreach (string name in SettingStore.GetSettingNames())
                LoadSetting(allowUserSettings, optionalValues, name);
            properties = null;
        }

        public override void Save()
        {
            foreach (string name in SettingStore.GetSettingNames())
                if (!Folders.ContainsKey(name))
                    Reset(name);
            foreach (string name in Folders.Keys)
                SaveSetting(name);
            properties = null;
        }

        public override void Reset()
        {
            while (Folders.Count > 0)
                Reset(Folders.Keys.First());
        }
    }
}
