﻿
using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.DebugInfo;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;
using Orts.Graphics.Xna;

namespace Orts.ActivityRunner.Viewer3D.Dispatcher.PopupWindows
{
    public enum DebugScreenInformation
    {
        Common,
    }

    public class DebugScreen : OverlayWindowBase
    {
        private readonly EnumArray<NameValueTextGrid, DebugScreenInformation> currentProvider = new EnumArray<NameValueTextGrid, DebugScreenInformation>();

        public DebugScreen(WindowManager owner, string caption, Color backgroundColor) :
            base(owner, caption, Point.Zero, Point.Zero)
        {
            ZOrder = 0;
            currentProvider[DebugScreenInformation.Common] = new NameValueTextGrid(this, (int)(10 * Owner.DpiScaling), (int)(30 * Owner.DpiScaling));
            UpdateBackgroundColor(backgroundColor);
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling)
        {
            foreach (NameValueTextGrid item in currentProvider)
            {
                layout?.Add(item);
            }
            return base.Layout(layout, headerScaling);
        }

        public void SetInformationProvider(DebugScreenInformation informationType, INameValueInformationProvider provider)
        {
            currentProvider[informationType].InformationProvider = provider;
        }

        public void UpdateBackgroundColor(Color backgroundColor)
        {
            foreach(NameValueTextGrid item in currentProvider)
                item.TextColor = backgroundColor.ComplementColor();
        }

        public override bool Open()
        {
            return base.Open();
        }

        public override bool Close()
        {
            return base.Close();
        }
    }
}
