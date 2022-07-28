﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Orts.Common;
using Orts.Graphics;
using Orts.Settings;
using Orts.Settings.Store;
using Orts.Toolbox.PopupWindows;

namespace Orts.Toolbox.Settings
{
    public class ToolboxSettings : SettingsBase
    {
        internal const string SettingLiteral = "Toolbox";

        private static readonly StoreType SettingsStoreType;
        private static readonly string Location;

        private PropertyInfo[] properties;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static ToolboxSettings()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            if (File.Exists(Location = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), EnumExtension.GetDescription(StoreType.Json))))
            {
                SettingsStoreType = StoreType.Json;
            }
            if (File.Exists(Location = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), EnumExtension.GetDescription(StoreType.Ini))))
            {
                SettingsStoreType = StoreType.Ini;
            }
            else
            {
                SettingsStoreType = StoreType.Registry;
                Location = EnumExtension.GetDescription(StoreType.Registry);
            }
        }

        public ToolboxSettings(IEnumerable<string> options) :
            this(options, SettingsStore.GetSettingsStore(SettingsStoreType, Location, null))
        {
        }

        internal ToolboxSettings(IEnumerable<string> options, SettingsStore store) :
            base(SettingsStore.GetSettingsStore(store.StoreType, store.Location, SettingLiteral))
        {
            LoadSettings(options);
            UserSettings = new UserSettings(options, store);
        }

        public UserSettings UserSettings { get; private set; }

        #region Toolbox Settings
        [Default(new string[]
        {
            $"{nameof(WindowSetting.Location)}=50,50",  // % of the windows Screen
            $"{nameof(WindowSetting.Size)}=75,75"    // % of screen size
        })]
        public EnumArray<int[], WindowSetting> WindowSettings { get; set; }

        [Default(0)]
        public int WindowScreen { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        [Default(new string[0])]
        public string[] RouteSelection { get; set; }

        [Default(new string[0])]
        public string[] PathSelection { get; set; }

        [Default(new string[0])]
        public string[] LastLocation { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [Default(true)]
        public bool RestoreLastView { get; set; }

        [Default(new string[]
        {
        $"{nameof(MapViewItemSettings.Tracks)}=True",
        $"{nameof(MapViewItemSettings.EndNodes)}=True",
        $"{nameof(MapViewItemSettings.JunctionNodes)}=True",
        $"{nameof(MapViewItemSettings.LevelCrossings)}=True",
        $"{nameof(MapViewItemSettings.CrossOvers)}=True",
        $"{nameof(MapViewItemSettings.Roads)}=True",
        $"{nameof(MapViewItemSettings.RoadEndNodes)}=True",
        $"{nameof(MapViewItemSettings.RoadCrossings)}=True",
        $"{nameof(MapViewItemSettings.CarSpawners)}=True",
        $"{nameof(MapViewItemSettings.Sidings)}=True",
        $"{nameof(MapViewItemSettings.SidingNames)}=True",
        $"{nameof(MapViewItemSettings.Platforms)}=True",
        $"{nameof(MapViewItemSettings.PlatformNames)}=True",
        $"{nameof(MapViewItemSettings.StationNames)}=True",
        $"{nameof(MapViewItemSettings.SpeedPosts)}=True",
        $"{nameof(MapViewItemSettings.MilePosts)}=True",
        $"{nameof(MapViewItemSettings.Signals)}=True",
        $"{nameof(MapViewItemSettings.OtherSignals)}=True",
        $"{nameof(MapViewItemSettings.Hazards)}=True",
        $"{nameof(MapViewItemSettings.Pickups)}=True",
        $"{nameof(MapViewItemSettings.SoundRegions)}=True",
        $"{nameof(MapViewItemSettings.Grid)}=True",
        $"{nameof(MapViewItemSettings.Paths)}=True",
        $"{nameof(MapViewItemSettings.PathEnds)}=True",
        $"{nameof(MapViewItemSettings.PathIntermediates)}=True",
        $"{nameof(MapViewItemSettings.PathJunctions)}=True",
        $"{nameof(MapViewItemSettings.PathReversals)}=True",
        })]
        public EnumArray<bool, MapViewItemSettings> ViewSettings { get; set; }

        [Default("{Application} Log.txt")]
        public string LogFilename { get; set; }

        [Default(new string[]{
            $"{nameof(ColorSetting.Background)}={nameof(Microsoft.Xna.Framework.Color.DarkGray)}",
            $"{nameof(ColorSetting.RailTrack)}={nameof(Microsoft.Xna.Framework.Color.Blue)}",
            $"{nameof(ColorSetting.RailTrackEnd)}={nameof(Microsoft.Xna.Framework.Color.BlueViolet)}",
            $"{nameof(ColorSetting.RailTrackJunction)}={nameof(Microsoft.Xna.Framework.Color.DarkMagenta)}",
            $"{nameof(ColorSetting.RailTrackCrossing)}={nameof(Microsoft.Xna.Framework.Color.Firebrick)}",
            $"{nameof(ColorSetting.RailLevelCrossing)}={nameof(Microsoft.Xna.Framework.Color.Crimson)}",
            $"{nameof(ColorSetting.RoadTrack)}={nameof(Microsoft.Xna.Framework.Color.Olive)}",
            $"{nameof(ColorSetting.RoadTrackEnd)}={nameof(Microsoft.Xna.Framework.Color.ForestGreen)}",
            $"{nameof(ColorSetting.RoadLevelCrossing)}={nameof(Microsoft.Xna.Framework.Color.DeepPink)}",
            $"{nameof(ColorSetting.PathTrack)}={nameof(Microsoft.Xna.Framework.Color.Gold)}",
            $"{nameof(ColorSetting.PathTrackEnd)}={nameof(Microsoft.Xna.Framework.Color.Gold)}",
            $"{nameof(ColorSetting.PathTrackIntermediate)}={nameof(Microsoft.Xna.Framework.Color.Gold)}",
            $"{nameof(ColorSetting.PathJunction)}={nameof(Microsoft.Xna.Framework.Color.Gold)}",
            $"{nameof(ColorSetting.PathReversal)}={nameof(Microsoft.Xna.Framework.Color.Gold)}",
            $"{nameof(ColorSetting.RoadCarSpawner)}={nameof(Microsoft.Xna.Framework.Color.White)}",
            $"{nameof(ColorSetting.SignalItem)}={nameof(Microsoft.Xna.Framework.Color.White)}",
            $"{nameof(ColorSetting.PlatformItem)}={nameof(Microsoft.Xna.Framework.Color.Navy)}",
            $"{nameof(ColorSetting.SidingItem)}={nameof(Microsoft.Xna.Framework.Color.ForestGreen)}",
            $"{nameof(ColorSetting.SpeedPostItem)}={nameof(Microsoft.Xna.Framework.Color.RoyalBlue)}",
            $"{nameof(ColorSetting.HazardItem)}={nameof(Microsoft.Xna.Framework.Color.White)}",
            $"{nameof(ColorSetting.PickupItem)}={nameof(Microsoft.Xna.Framework.Color.White)}",
            $"{nameof(ColorSetting.SoundRegionItem)}={nameof(Microsoft.Xna.Framework.Color.White)}",
            $"{nameof(ColorSetting.LevelCrossingItem)}={nameof(Microsoft.Xna.Framework.Color.White)}",
        })]
        public EnumArray<string, ColorSetting> ColorSettings { get; set; }

        [Default(new string[]
        {
            $"{nameof(WindowType.QuitWindow)}=50,50",
            $"{nameof(WindowType.AboutWindow)}=50,50",
            $"{nameof(WindowType.StatusWindow)}=50,50",
            $"{nameof(WindowType.DebugScreen)}=0,0",
            $"{nameof(WindowType.LocationWindow)}=100,100",
            $"{nameof(WindowType.HelpWindow)}=10,90",
            $"{nameof(WindowType.TrackNodeInfoWindow)}=10,70",
            $"{nameof(WindowType.SettingsWindow)}=70,70",
            $"{nameof(WindowType.LogWindow)}=30,70",
        })]
        public EnumArray<int[], WindowType> PopupLocations { get; set; }

        [Default(new string[]
        {
            $"{nameof(WindowType.QuitWindow)}=False",
            $"{nameof(WindowType.AboutWindow)}=False",
            $"{nameof(WindowType.StatusWindow)}=False",
            $"{nameof(WindowType.DebugScreen)}=False",
            $"{nameof(WindowType.LocationWindow)}=True",
            $"{nameof(WindowType.HelpWindow)}=True",
            $"{nameof(WindowType.TrackNodeInfoWindow)}=True",
            $"{nameof(WindowType.SettingsWindow)}=True",
            $"{nameof(WindowType.LogWindow)}=False",
        })]
        public EnumArray<bool, WindowType> PopupStatus { get; set; }

        [Default(new string[]
        {
            $"{nameof(WindowType.QuitWindow)}=\"\"",
            $"{nameof(WindowType.AboutWindow)}=",
            $"{nameof(WindowType.StatusWindow)}=",
            $"{nameof(WindowType.DebugScreen)}=\"\"",
            $"{nameof(WindowType.LocationWindow)}=\"\"",
            $"{nameof(WindowType.HelpWindow)}=",
            $"{nameof(WindowType.TrackNodeInfoWindow)}=",
            $"{nameof(WindowType.SettingsWindow)}=",
            $"{nameof(WindowType.LogWindow)}=",
        })]
        public EnumArray<string, WindowType> PopupSettings { get; set; }


        [Default("Segoe UI")]
        public string TextFont { get; set; }

        [Default(13)]
        public int FontSize { get; set; }
        #endregion

        public override object GetDefaultValue(string name)
        {
            PropertyInfo property = GetProperty(name);
            object defaultValue = property.GetCustomAttributes<DefaultAttribute>(false).FirstOrDefault()?.Value;
            Type propertyType = property.PropertyType;
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(EnumArray<,>).GetGenericTypeDefinition())
            {
                defaultValue = InitializeEnumArrayDefaults(propertyType, defaultValue);
            }
            return defaultValue ?? throw new InvalidDataException($"Toolbox setting {property.Name} has no default value.");
        }

        protected override PropertyInfo[] GetProperties()
        {
            if (properties == null)
                properties = base.GetProperties().Where(pi => !new string[] { "UserSettings" }.Contains(pi.Name)).ToArray();
            return properties;
        }

        public override void Reset()
        {
            foreach (PropertyInfo property in GetProperties())
                Reset(property.Name);
        }

        public override void Save()
        {
            foreach (PropertyInfo property in GetProperties())
                Save(property.Name);
        }

        protected override object GetValue(string name)
        {
            return GetProperty(name).GetValue(this, null);
        }

        protected override void Load(bool allowUserSettings, NameValueCollection optionalValues)
        {
            foreach (PropertyInfo property in GetProperties())
                LoadSetting(allowUserSettings, optionalValues, property.Name);
            ResetCachedProperties();
        }

        protected override void SetValue(string name, object value)
        {
            GetProperty(name).SetValue(this, value, null);
        }
    }
}
