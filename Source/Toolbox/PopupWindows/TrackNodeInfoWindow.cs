﻿
using Microsoft.Xna.Framework;

using Orts.Common.Input;
using Orts.Graphics.MapView;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;

namespace Orts.Toolbox.PopupWindows
{
    internal class TrackNodeInfoWindow : FramedWindowBase
    {
        private ContentArea contentArea;
        private readonly UserCommandController<UserCommand> userCommandController;
        private NameValueTextGrid trackNodeInfoGrid;

        public TrackNodeInfoWindow(WindowManager owner, ContentArea contentArea, Point relativeLocation) :
            base(owner, "Track Node Information", relativeLocation, new Point(240, 174))
        {
            this.contentArea = contentArea;
            userCommandController = Owner.UserCommandController as UserCommandController<UserCommand>;
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            layout = base.Layout(layout, headerScaling);
            layout = layout.AddLayoutVertical();
            trackNodeInfoGrid = new NameValueTextGrid(this, 0, 0, layout.RemainingWidth, layout.RemainingHeight)
            {
                InformationProvider = contentArea?.Content.TrackNodeInfo,
                ColumnWidth = layout.RemainingWidth / 2,
            };
            layout.Add(trackNodeInfoGrid);
            return layout;
        }

        internal void GameWindow_OnContentAreaChanged(object sender, ContentAreaChangedEventArgs e)
        {
            contentArea = e.ContentArea;
            trackNodeInfoGrid.InformationProvider = contentArea?.Content?.TrackNodeInfo;            
        }

        private void TabAction(UserCommandArgs args)
        {
            if (args is ModifiableKeyCommandArgs keyCommandArgs && (keyCommandArgs.AdditionalModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            {
            }
        }

        public override bool Open()
        {
            userCommandController.AddEvent(UserCommand.DisplayLocationWindow, KeyEventType.KeyPressed, TabAction, true);
            return base.Open();
        }

        public override bool Close()
        {
            userCommandController.RemoveEvent(UserCommand.DisplayLocationWindow, KeyEventType.KeyPressed, TabAction);
            return base.Close();
        }
    }
}
