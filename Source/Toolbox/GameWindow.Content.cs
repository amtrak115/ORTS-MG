﻿using System;
using System.Collections.Frozen;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Graphics.MapView;
using FreeTrainSimulator.Graphics.Xna;
using FreeTrainSimulator.Models.Content;
using FreeTrainSimulator.Models.Shim;
using FreeTrainSimulator.Models.Imported.Shim;
using FreeTrainSimulator.Toolbox.PopupWindows;

using Microsoft.Xna.Framework;
using FreeTrainSimulator.Models.Settings;

namespace FreeTrainSimulator.Toolbox
{
    public class ContentAreaChangedEventArgs : EventArgs
    {
        public ContentArea ContentArea { get; }

        public ContentAreaChangedEventArgs(ContentArea contentArea)
        {
            ContentArea = contentArea;
        }
    }

    public partial class GameWindow : Game
    {
        private ContentModel contentModel;
        private FolderModel selectedFolder;
        private RouteModelCore selectedRoute;
        private FrozenSet<RouteModelCore> routeModels;
        private readonly SemaphoreSlim loadRouteSemaphore = new SemaphoreSlim(1);
        private CancellationTokenSource ctsProfileLoading;
        private CancellationTokenSource ctsRouteLoading;
        private PathEditor pathEditor;

        internal PathEditor PathEditor
        {
            get
            {
                if (null == pathEditor && contentArea != null)
                {
                    pathEditor = new PathEditor(contentArea);
                    pathEditor.OnPathChanged += PathEditor_OnEditorPathChanged;
                }
                return pathEditor;
            }
        }

        private void PathEditor_OnEditorPathChanged(object sender, PathEditorChangedEventArgs e)
        {
//            mainmenu.PreSelectPath(e.Path?.FilePath);
        }

        internal async Task LoadFolders()
        {
            ctsProfileLoading = await ctsProfileLoading.ResetCancellationTokenSource(loadRouteSemaphore, true).ConfigureAwait(false);

            try
            {
                contentModel = await contentModel.Get(ctsProfileLoading.Token).ConfigureAwait(false);
                mainmenu.PopulateContentFolders(contentModel.ContentFolders);
            }
            catch (TaskCanceledException)
            {
                mainmenu.PopulateContentFolders(FrozenSet<FolderModel>.Empty);
            }
        }

        internal async Task<FrozenSet<RouteModelCore>> FindRoutes(FolderModel contentFolder)
        {
            ctsProfileLoading = await ctsProfileLoading.ResetCancellationTokenSource(loadRouteSemaphore, true).ConfigureAwait(false);
            await loadRouteSemaphore.WaitAsync().ConfigureAwait(false);
            if (contentFolder != selectedFolder)
            {
                try
                {
                    routeModels = await contentFolder.GetRoutes(ctsProfileLoading.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { }
                selectedFolder = contentFolder;
            }
            loadRouteSemaphore.Release();
            return routeModels;
        }

        internal async Task LoadRoute(RouteModelCore route)
        {
            (windowManager[ToolboxWindowType.StatusWindow] as StatusTextWindow).RouteName = route.Name;
            windowManager[ToolboxWindowType.StatusWindow].Open();
            UnloadRoute();

            ctsRouteLoading = await ctsRouteLoading.ResetCancellationTokenSource(loadRouteSemaphore, true).ConfigureAwait(false);

            bool? useMetricUnits = ToolboxUserSettings.MeasurementUnit == MeasurementUnit.Metric || (ToolboxUserSettings.MeasurementUnit == MeasurementUnit.System && System.Globalization.RegionInfo.CurrentRegion.IsMetric);
            if (ToolboxUserSettings.MeasurementUnit == MeasurementUnit.Route)
                useMetricUnits = null;

            RouteModel routeModel = await route.Extend(ctsProfileLoading.Token).ConfigureAwait(false);

            await TrackData.LoadTrackData(this, routeModel, useMetricUnits, ctsProfileLoading.Token).ConfigureAwait(false);
            if (ctsProfileLoading.Token.IsCancellationRequested)
                return;

            ToolboxContent content = new ToolboxContent(this);
            await content.Initialize().ConfigureAwait(false);
            content.InitializeItemVisiblity(ToolboxSettings.ViewSettings);
            content.UpdateWidgetColorSettings(ToolboxSettings.ColorSettings);
            content.ContentArea.FontOutlineOptions = ToolboxSettings.FontOutline ? OutlineRenderOptions.Default : null;
            ContentArea = content.ContentArea;
            mainmenu.PopulatePaths((Orts.Formats.Msts.RuntimeData.GameInstance(this) as TrackData).TrainPaths);
            windowManager[ToolboxWindowType.StatusWindow].Close();
            selectedRoute = route;
        }

        internal bool LoadPath(PathModelCore path)
        {
            return PathEditor.InitializePath(path);
        }

        internal void EditPath()
        {
            PathEditor.InitializeNewPath();
        }

        internal async Task PreSelectRoute(string folderName, string routeId, string pathId)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                FolderModel folder = mainmenu.SelectContentFolder(folderName);

                if (!string.IsNullOrEmpty(routeId) && ToolboxSettings.RestoreLastView)
                {
                    RouteModelCore route = (routeModels ??= await FindRoutes(folder).ConfigureAwait(false))?.GetById(routeId);
                    if (null != route)
                    {
                        await LoadRoute(route).ConfigureAwait(false);
                        mainmenu.PreSelectRoute(route.Name);
                        if (!string.IsNullOrEmpty(pathId))
                        {
                            // only restore first path for now
                            PathModelCore path = (await route.GetRoutePaths(CancellationToken.None).ConfigureAwait(false)).GetById(pathId);
                            if (null != path)
                            {
                                if (LoadPath(path))
                                    mainmenu.PreSelectPath(path);
                            }
                        }
                    }
                }
            }
        }

        internal void UnloadRoute()
        {
            ContentArea = null;
            selectedRoute = null;
            mainmenu.ClearPathMenu();
            pathEditor?.Dispose();
            pathEditor = null;
        }

        internal void UnloadPath()
        {
            PathEditor.InitializePath(null);
        }
    }
}
