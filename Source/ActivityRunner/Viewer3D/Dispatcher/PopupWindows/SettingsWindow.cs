﻿
using GetText;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;
using Orts.Settings;

namespace Orts.ActivityRunner.Viewer3D.Dispatcher.PopupWindows
{
    internal class SettingsWindow : WindowBase
    {
        private readonly DispatcherSettings settings;
        private Checkbox chkShowPlatforms;
        private Checkbox chkShowStations;
        private Checkbox chkShowSidings;

        public SettingsWindow(WindowManager owner, DispatcherSettings settings, 
            Point relativeLocation, Catalog catalog = null) : base(owner, "Settings", relativeLocation, new Point(200, 200), catalog)
        {
            this.settings = settings;
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            layout = base.Layout(layout, headerScaling);
            layout = layout.AddLayoutScrollboxVertical(layout.RemainingWidth);

            ControlLayoutHorizontal line = layout.AddLayoutHorizontalLineOfText();
            int width = (int)(line.RemainingWidth * 0.8);
            line.Add(new Label(this, width, line.RemainingHeight, Catalog.GetString("Show Platform Names")));
            chkShowPlatforms = new Checkbox(this);
            chkShowPlatforms.OnClick += (object sender, MouseClickEventArgs e) => settings.ViewSettings[MapViewItemSettings.PlatformNames] = (sender as Checkbox).State.Value;
            chkShowPlatforms.State = settings.ViewSettings[MapViewItemSettings.PlatformNames];
            line.Add(chkShowPlatforms);

            line = layout.AddLayoutHorizontalLineOfText();
            line.Add(new Label(this, width, line.RemainingHeight, Catalog.GetString("Show Siding Names")));
            chkShowSidings = new Checkbox(this);
            chkShowSidings.OnClick += (object sender, MouseClickEventArgs e) => settings.ViewSettings[MapViewItemSettings.SidingNames] = (sender as Checkbox).State.Value;
            chkShowSidings.State = settings.ViewSettings[MapViewItemSettings.SidingNames];
            line.Add(chkShowSidings);

            line = layout.AddLayoutHorizontalLineOfText();
            line.Add(new Label(this, width, line.RemainingHeight, Catalog.GetString("Show Station Names")));
            chkShowStations = new Checkbox(this);
            chkShowStations.OnClick += (object sender, MouseClickEventArgs e) => settings.ViewSettings[MapViewItemSettings.StationNames] = (sender as Checkbox).State.Value;
            chkShowStations.State = settings.ViewSettings[MapViewItemSettings.StationNames];
            line.Add(chkShowStations);

            return layout;
        }
    }
}
