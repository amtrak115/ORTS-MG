﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using GetText;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.Info;
using Orts.Common.Input;
using Orts.Formats.Msts;
using Orts.Formats.Msts.Models;
using Orts.Graphics;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;
using Orts.Settings;
using Orts.Settings.Util;
using Orts.Simulation;
using Orts.Simulation.Activities;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Timetables;

namespace Orts.ActivityRunner.Viewer3D.PopupWindows
{
    internal class HelpWindow : WindowBase
    {
        private enum TabSettings
        {
            [Description("Key Commands")]
            KeyboardShortcuts,
            [Description("Briefing")]
            ActivityBriefing,
            [Description("Timetable")]
            ActivityTimetable,
            [Description("Work Orders")]
            ActivityWorkOrders,
            [Description("Evaluation")]
            ActivityEvaluation,
            [Description("Briefing")]
            TimetableBriefing,
            [Description("Procedures")]
            LocomotiveProcedures,
        }

        private enum EvaluationTabSettings
        {
            [Description("Overview")]
            Overview,
            [Description("Details")]
            Details,
            [Description("Report")]
            Report,
        }

        private TabControl<TabSettings> tabControl;
        private readonly UserCommandController<UserCommand> userCommandController;
        private readonly UserSettings settings;
        private readonly Viewer viewer;

        private ActivityTask lastActivityTask;
        private bool stoppedAt;
        private int lastLastEventID = -1;
        private ControlLayout activityTimetableScrollbox;
        private ControlLayout activityWorkOrderScrollbox;
        private ControlLayout evaluationTab;
        private TextBox reportText;
        private bool evaluationCompleted;

        public HelpWindow(WindowManager owner, Point relativeLocation, UserSettings settings, Viewer viewer, Catalog catalog = null) :
            base(owner, (catalog ??= CatalogManager.Catalog).GetString("Help"), relativeLocation, new Point(560, 380), catalog)
        {
            userCommandController = viewer.UserCommandController;
            this.settings = settings;
            this.viewer = viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            layout = base.Layout(layout, headerScaling);
            tabControl = new TabControl<TabSettings>(this, layout.RemainingWidth, layout.RemainingHeight, true);
            #region Keyboard tab
            tabControl.TabLayouts[TabSettings.KeyboardShortcuts] = (layoutContainer) =>
            {
                System.Drawing.Font keyFont = FontManager.Scaled(Owner.DefaultFontName, System.Drawing.FontStyle.Regular)[Owner.DefaultFontSize - 1];
                layoutContainer = layoutContainer.AddLayoutScrollboxVertical(layoutContainer.RemainingWidth);
                layoutContainer.HorizontalChildAlignment = HorizontalAlignment.Center;

                int keyWidth = layoutContainer.RemainingWidth / KeyboardMap.MapWidth;
                int keyHeight = 3 * keyWidth + 2;
                ControlLayout keyboardMap = layoutContainer.AddLayoutPanel(KeyboardMap.MapWidth * keyWidth, keyHeight * KeyboardMap.KeyboardLayout.Length);
                KeyboardMap.DrawKeyboardMap((keyBox, keyScanCode, keyName) =>
                {
                    Color color = KeyboardMap.GetScanCodeColor(KeyboardMap.GetScanCodeCommands(keyScanCode, settings.Input.UserCommands));
                    if (color == Color.Transparent)
                        color = Color.Black;
                    KeyboardMap.Scale(ref keyBox, keyWidth, keyHeight);
                    keyboardMap.Add(new KeyLabel(this, keyBox.Left - keyboardMap.CurrentLeft, keyBox.Top - keyboardMap.CurrentTop, keyBox.Width - 1, keyBox.Height - 1, keyName, keyFont, color));
                });

                layoutContainer.AddHorizontalSeparator();
                ControlLayoutHorizontal headerLine = layoutContainer.AddLayoutHorizontalLineOfText();
                int width = headerLine.RemainingWidth / 2;
                headerLine.Add(new Label(this, width, headerLine.RemainingHeight, Catalog.GetString("Function")));
                headerLine.Add(new Label(this, width, headerLine.RemainingHeight, Catalog.GetString("Key")));
                layoutContainer.AddHorizontalSeparator();
                foreach (UserCommand command in EnumExtension.GetValues<UserCommand>())
                {
                    ControlLayoutHorizontal line = layoutContainer.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(this, width, line.RemainingHeight, command.GetLocalizedDescription()));
                    line.Add(new Label(this, width, line.RemainingHeight, settings.Input.UserCommands[command].ToString()));
                }
            };
            #endregion
            if (Simulator.Instance.ActivityFile != null)
            {
                #region Activity Briefing tab
                tabControl.TabLayouts[TabSettings.ActivityBriefing] = (layoutContainer) =>
                {
                    TextBox activityBriefing = new TextBox(this, layoutContainer.RemainingWidth, layoutContainer.RemainingHeight, Simulator.Instance.ActivityFile.Activity?.Header?.Briefing, true);
                    layoutContainer.Add(activityBriefing);
                };
                #endregion
                #region Activity Timetable tab
                tabControl.TabLayouts[TabSettings.ActivityTimetable] = (layoutContainer) =>
                {
                    int columnWidth = layoutContainer.RemainingWidth / 7;
                    var line = layoutContainer.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(this, columnWidth * 3, line.RemainingHeight, Catalog.GetString("Station")));
                    line.Add(new Label(this, columnWidth, line.RemainingHeight, Catalog.GetString("Arrive"), HorizontalAlignment.Center));
                    line.Add(new Label(this, columnWidth, line.RemainingHeight, Catalog.GetString("Actual"), HorizontalAlignment.Center));
                    line.Add(new Label(this, columnWidth, line.RemainingHeight, Catalog.GetString("Depart"), HorizontalAlignment.Center));
                    line.Add(new Label(this, columnWidth, line.RemainingHeight, Catalog.GetString("Actual"), HorizontalAlignment.Center));
                    layoutContainer.AddHorizontalSeparator();
                    activityTimetableScrollbox = layoutContainer.AddLayoutScrollboxVertical(layoutContainer.RemainingWidth);

                    if (Simulator.Instance.ActivityRun != null)
                    {
                        foreach (ActivityTaskPassengerStopAt activityTask in Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>())
                        {
                            Label actualArrival, actualDeparture;
                            line = activityTimetableScrollbox.AddLayoutHorizontalLineOfText();
                            line.Add(new Label(this, columnWidth * 3, line.RemainingHeight, activityTask.PlatformEnd1.Station));
                            line.Add(new Label(this, columnWidth, line.RemainingHeight, $"{activityTask.ScheduledArrival}", HorizontalAlignment.Center));
                            line.Add(actualArrival = new Label(this, columnWidth, line.RemainingHeight,
                                $"{(activityTask.ActualArrival.HasValue ? activityTask.ActualArrival : activityTask.IsCompleted.HasValue && activityTask.NextTask != null ? Catalog.GetString("(missed)") : string.Empty)}", HorizontalAlignment.Center)
                            { TextColor = ColorCoding.ArrivalColor(activityTask.ScheduledArrival, activityTask.ActualArrival) });
                            line.Add(new Label(this, columnWidth, line.RemainingHeight, $"{activityTask.ScheduledDeparture}", HorizontalAlignment.Center));
                            line.Add(actualDeparture = new Label(this, columnWidth, line.RemainingHeight,
                                $"{(activityTask.ActualDeparture.HasValue ? activityTask.ActualDeparture : activityTask.IsCompleted.HasValue && activityTask.NextTask != null ? Catalog.GetString("(missed)") : string.Empty)}", HorizontalAlignment.Center)
                            { TextColor = ColorCoding.DepartureColor(activityTask.ScheduledDeparture, activityTask.ActualDeparture) });
                            line.Tag = (activityTask, actualArrival, actualDeparture);
                        }
                    }
                };
                #endregion
                #region Activity Work Orders
                tabControl.TabLayouts[TabSettings.ActivityWorkOrders] = (layoutContainer) =>
                {
                    int columnWidth = layoutContainer.RemainingWidth / 20;
                    {
                        ControlLayout line = layoutContainer.AddLayoutHorizontalLineOfText();
                        line.Add(new Label(this, columnWidth * 4, line.RemainingHeight, Catalog.GetString("Task")));
                        line.Add(new Label(this, columnWidth * 6, line.RemainingHeight, Catalog.GetString("Car(s)")));
                        line.Add(new Label(this, columnWidth * 7, line.RemainingHeight, Catalog.GetString("Location")));
                        line.Add(new Label(this, columnWidth * 6, line.RemainingHeight, Catalog.GetString("Status")));
                    }
                    layoutContainer.AddHorizontalSeparator();
                    activityWorkOrderScrollbox = layoutContainer.AddLayoutScrollboxVertical(layoutContainer.RemainingWidth);
                    List<(EventWrapper, Label)> activityEvents = new List<(EventWrapper, Label)>();
                    activityWorkOrderScrollbox.Tag = activityEvents;
                    bool addSeparator = false;
                    foreach (EventWrapper eventWrapper in Simulator.Instance.ActivityRun.EventList ?? Enumerable.Empty<EventWrapper>())
                    {
                        if (eventWrapper.ActivityEvent is ActionActivityEvent activityEvent)
                        {
                            if (addSeparator)
                                activityWorkOrderScrollbox.AddHorizontalSeparator();
                            ControlLayout line = activityWorkOrderScrollbox.AddLayoutHorizontalLineOfText();
                            // Task column
                            switch (activityEvent.Type)
                            {
                                case EventType.AssembleTrain:
                                case EventType.AssembleTrainAtLocation:
                                    line.Add(new Label(this, columnWidth * 4, line.RemainingHeight, Catalog.GetString("Assemble Train")));
                                    if (activityEvent.Type == EventType.AssembleTrainAtLocation)
                                    {
                                        line = activityWorkOrderScrollbox.AddLayoutHorizontalLineOfText();
                                        line.Add(new Label(this, columnWidth * 4, line.RemainingHeight, Catalog.GetString("At Location")));
                                    }
                                    break;
                                case EventType.DropOffWagonsAtLocation:
                                    line.Add(new Label(this, columnWidth * 4, line.RemainingHeight, Catalog.GetString("Drop Off")));
                                    break;
                                case EventType.PickUpPassengers:
                                case EventType.PickUpWagons:
                                    line.Add(new Label(this, columnWidth * 4, line.RemainingHeight, Catalog.GetString("Pick Up")));
                                    break;
                            }
                            if (activityEvent.WorkOrderWagons != null)
                            {
                                string location = "";
                                bool locationShown = false;
                                int wagonIdx = 0;
                                string locationFirst = "";
                                foreach (WorkOrderWagon wagonItem in activityEvent.WorkOrderWagons)
                                {
                                    if (locationShown)
                                    {
                                        line = activityWorkOrderScrollbox.AddLayoutHorizontalLineOfText();
                                        line.AddSpace(columnWidth * 4, 0);
                                    }

                                    // Car(s) column
                                    // Wagon.UiD contains train and wagon indexes packed into single 32-bit value, e.g. 32678 - 0
                                    uint trainIndex = wagonItem.UiD >> 16;         // Extract upper 16 bits
                                    uint wagonIndex = wagonItem.UiD & 0x0000FFFF;  // Extract lower 16 bits
                                    string wagonName = $"{trainIndex} - {wagonIndex}";
                                    string wagonType = "";
                                    bool wagonFound = false;
                                    if (Simulator.Instance.ActivityFile.Activity.ActivityObjects != null)
                                    {
                                        foreach (ActivityObject activityObject in Simulator.Instance.ActivityFile.Activity.ActivityObjects)
                                        {
                                            if (activityObject.ID == trainIndex)
                                            {
                                                foreach (Wagon trainWagon in activityObject.TrainSet.Wagons)
                                                {
                                                    if (trainWagon.UiD == wagonIndex)
                                                    {
                                                        wagonType = trainWagon.Name;
                                                        wagonFound = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (wagonFound)
                                                break;
                                        }
                                        if (!wagonFound)
                                        {
                                            foreach (var car in Simulator.Instance.PlayerLocomotive.Train.Cars)
                                            {
                                                if (car.UiD == wagonItem.UiD)
                                                {
                                                    wagonType = Path.GetFileNameWithoutExtension(car.WagFilePath);
                                                    wagonFound = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    line.Add(new Label(this, columnWidth * 6, line.RemainingHeight, $"{wagonName} ({wagonType})"));

                                    // Location column
                                    if (locationShown &&
                                        !((activityEvent.Type == EventType.PickUpPassengers) || (activityEvent.Type == EventType.PickUpWagons)))
                                    {
                                        line.AddSpace(columnWidth * 7, 0);
                                    }
                                    else
                                    {
                                        int sidingId = activityEvent.Type == EventType.AssembleTrainAtLocation || activityEvent.Type == EventType.DropOffWagonsAtLocation
                                            ? activityEvent.SidingId : wagonItem.SidingId;
                                        foreach (TrackItem item in RuntimeData.Instance.TrackDB.TrackItems)
                                        {
                                            if (item is SidingItem siding && siding.TrackItemId == sidingId)
                                            {
                                                location = siding.ItemName;
                                                break;
                                            }
                                        }
                                        if (locationFirst != location)
                                        {
                                            line.Add(new Label(this, columnWidth * 7, line.RemainingHeight, location));
                                        }
                                        else if (location.Length == 0 | (activityEvent.Type == EventType.PickUpPassengers) || (activityEvent.Type == EventType.PickUpWagons))
                                            line.AddSpace(columnWidth * 7, 0);
                                        locationFirst = location;
                                        locationShown = true;
                                    }
                                    // Status column
                                    if (wagonIdx == 0)
                                    {
                                        Label statusLabel;
                                        line.Add(statusLabel = new Label(this, columnWidth * 6, line.RemainingHeight, eventWrapper.TimesTriggered == 1 ? "Done" : string.Empty));
                                        activityEvents.Add((eventWrapper, statusLabel));
                                    }
                                    wagonIdx++;

                                    addSeparator = true;
                                }
                            }
                        }

                    }
                };
                #endregion
                #region Activity Evaluation
                if (settings.ActivityEvalulation && Simulator.Instance.ActivityRun != null)
                {
                    tabControl.TabLayouts[TabSettings.ActivityEvaluation] = (layoutContainer) =>
                    {
                        List<(Label, Func<(string, Color)>)> activityEvaluation = new List<(Label, Func<(string, Color)>)>();
                        Label functionLabel;
                        evaluationTab = new TabControl<EvaluationTabSettings>(this, layoutContainer.RemainingWidth, layoutContainer.RemainingHeight, true);
                        evaluationTab.Tag = activityEvaluation;
                        (evaluationTab as TabControl<EvaluationTabSettings>).TabLayouts[EvaluationTabSettings.Overview] = (evaluationLayoutContainer) =>
                        {
                            evaluationLayoutContainer = evaluationLayoutContainer.AddLayoutScrollboxVertical(evaluationLayoutContainer.RemainingWidth);
                            int columnWidth = evaluationLayoutContainer.RemainingWidth / 3;
                            AddEvaluationLine(evaluationLayoutContainer, "Activity:", Simulator.Instance.ActivityFile.Activity.Header.Name);
                            AddEvaluationLine(evaluationLayoutContainer, "Start Time:", Simulator.Instance.ActivityFile.Activity.Header.StartTime.ToString());
                            AddEvaluationLine(evaluationLayoutContainer, "Estimated Duration:", Simulator.Instance.ActivityFile.Activity.Header.Duration.ToString());
                            evaluationLayoutContainer.AddHorizontalSeparator();
                            AddEvaluationLine(evaluationLayoutContainer, "Timetable:", null);
                            int stationStops = Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Count();
                            AddEvaluationLine(evaluationLayoutContainer, "Station Stops:", $"{stationStops}", 20);
                            Func<(string text, Color textColor)> remainingStopsFunc = () =>
                            {
                                return ($"{Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Where((stopTask) => !stopTask.ActualArrival.HasValue).Count()}", Color.White);
                            };
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Remaining Stops:", remainingStopsFunc().text, 20);
                            activityEvaluation.Add((functionLabel, remainingStopsFunc));

                            Func<(string text, Color textColor)> delayFunc = () =>
                            {
                                TimeSpan delay = Simulator.Instance.PlayerLocomotive.Train.Delay ?? TimeSpan.Zero;
                                return ($"{delay}", delay.TotalSeconds switch
                                {
                                    > 120 => Color.OrangeRed,
                                    > 60 => Color.LightSalmon,
                                    0 => Color.White,
                                    < 0 => Color.LightSalmon,
                                    _ => Color.LightGreen,
                                });
                            };
                            (string text, Color textColor) = delayFunc();
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Current Delay:", text, textColor, 20);
                            activityEvaluation.Add((functionLabel, delayFunc));

                            Func<(string text, Color textColor)> missedStopsFunc = () =>
                            {
                                int count = Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Where((stopTask) => !(stopTask.ActualArrival.HasValue || !stopTask.ActualDeparture.HasValue) && stopTask.IsCompleted.HasValue && stopTask.NextTask != null).Count();
                                return ($"{count}", count > 0 ? Color.LightSalmon : Color.White);
                            };
                            (string text, Color textColor) missedStops = missedStopsFunc();
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Missed Stops:", missedStops.text, missedStops.textColor, 20);
                            activityEvaluation.Add((functionLabel, missedStopsFunc));

                            foreach (ActivityTaskPassengerStopAt item in Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Where((stopTask) => !(stopTask.ActualArrival.HasValue || !stopTask.ActualDeparture.HasValue) && stopTask.IsCompleted.HasValue && stopTask.NextTask != null))
                            {
                                ControlLayout line = evaluationLayoutContainer.AddLayoutHorizontalLineOfText();
                                line.Add(new Label(this, columnWidth, line.RemainingHeight, $"\t{item.PlatformEnd1.ItemName}") { TextColor = Color.LightSalmon });
                            }
                            evaluationLayoutContainer.AddHorizontalSeparator();
                            AddEvaluationLine(evaluationLayoutContainer, "Work orders:", null);

                            int taskCount = Simulator.Instance.ActivityRun.EventList.Select((wrapper) => wrapper.ActivityEvent).OfType<ActionActivityEvent>()
                                .Where((activityTask) => activityTask.Type != EventType.AllStops && activityTask.Type != EventType.ReachSpeed).Count();
                            AddEvaluationLine(evaluationLayoutContainer, "Tasks:", $"{taskCount}", 20);

                            Func<(string text, Color textColor)> taskDoneFunc = () =>
                            {
                                int count = Simulator.Instance.ActivityRun.EventList.Where((wrapper) => wrapper.TimesTriggered == 1).Select((wrapper) => wrapper.ActivityEvent).OfType<ActionActivityEvent>().
                                    Where((activityTask) => activityTask.Type != EventType.AllStops && activityTask.Type != EventType.ReachSpeed).Count();
                                return ($"{count}", Color.White);
                            };
                            (string text, Color textColor) taskDone = taskDoneFunc();
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Accomplished:", taskDone.text, 20);
                            activityEvaluation.Add((functionLabel, taskDoneFunc));
                            Func<(string text, Color textColor)> couplerSpeedFunc = () =>
                            {
                                return ($"{ActivityEvaluation.Instance.OverSpeedCoupling}", Color.White);
                            };
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Coupling speed exceeded:", couplerSpeedFunc().text, 20);
                            activityEvaluation.Add((functionLabel, couplerSpeedFunc));
                        };
                        (evaluationTab as TabControl<EvaluationTabSettings>).TabLayouts[EvaluationTabSettings.Details] = (evaluationLayoutContainer) =>
                        {
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Alerter applications above 10Mph/16kmh", $"= {ActivityEvaluation.Instance.FullBrakeAbove16kmh}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.FullBrakeAbove16kmh}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Auto pilot Time", $"= {FormatStrings.FormatTime(ActivityEvaluation.Instance.AutoPilotTime)}");
                            activityEvaluation.Add((functionLabel, () => ($"= {FormatStrings.FormatTime(ActivityEvaluation.Instance.AutoPilotTime)}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, Simulator.Instance.Settings.BreakCouplers ? "Coupler breaks" : "Coupler overloaded", $"= {ActivityEvaluation.Instance.CouplerBreaks}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.CouplerBreaks}", Color.White)));
                            if (Simulator.Instance.Settings.CurveSpeedDependent)
                            {
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Curve speeds exceeded", $"= {ActivityEvaluation.Instance.TravellingTooFast}");
                                activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.TravellingTooFast}", Color.White)));
                            }
                            if (Simulator.Instance.PlayerLocomotive.Train.Delay.HasValue)
                            {
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Current delay in Activity", $"= {Simulator.Instance.PlayerLocomotive.Train.Delay}");
                                activityEvaluation.Add((functionLabel, () => ($"= {Simulator.Instance.PlayerLocomotive.Train.Delay}", Color.White)));
                            }
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Departure before boarding completed", $"= {ActivityEvaluation.Instance.DepartBeforeBoarding}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.DepartBeforeBoarding}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Distance travelled", $"= {FormatStrings.FormatDistanceDisplay(ActivityEvaluation.Instance.DistanceTravelled, Simulator.Instance.MetricUnits)}");
                            activityEvaluation.Add((functionLabel, () => ($"= {FormatStrings.FormatDistanceDisplay(ActivityEvaluation.Instance.DistanceTravelled, Simulator.Instance.MetricUnits)}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Emergency applications while moving", $"= {ActivityEvaluation.Instance.EmergencyButtonMoving}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.EmergencyButtonMoving}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Emergency applications while stopped", $"= {ActivityEvaluation.Instance.EmergencyButtonStopped}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.EmergencyButtonStopped}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Full Train Brake applications under 5MPH/8KMH", $"= {ActivityEvaluation.Instance.FullTrainBrakeUnder8kmh}");
                            if (Simulator.Instance.Settings.CurveSpeedDependent)
                            {
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Hose breaks", $"= {ActivityEvaluation.Instance.SnappedBrakeHose}");
                                activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.SnappedBrakeHose}", Color.White)));
                            }
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Over Speed", $"= {ActivityEvaluation.Instance.OverSpeed}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.OverSpeed}", Color.White)));
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Over Speed Time", $"= {FormatStrings.FormatTime(ActivityEvaluation.Instance.OverSpeedTime)}");
                            activityEvaluation.Add((functionLabel, () => ($"= {FormatStrings.FormatTime(ActivityEvaluation.Instance.OverSpeedTime)}", Color.White)));

                            if (Simulator.Instance.ActivityRun?.Tasks.OfType<ActivityTaskPassengerStopAt>().Count() > 0)
                            {
                                Func<(string text, Color textColor)> missedStopsFunc = () =>
                                {
                                    return ($"= {Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Where((stopTask) => !(stopTask.ActualArrival.HasValue || !stopTask.ActualDeparture.HasValue) && stopTask.IsCompleted.HasValue && stopTask.NextTask != null).Count()}", Color.White);
                                };
                                (string text, Color textColor) missedStops = missedStopsFunc();
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Station Stops missed:", missedStops.text, missedStops.textColor, 20);
                                activityEvaluation.Add((functionLabel, missedStopsFunc));
                                Func<(string text, Color textColor)> remainingStopsFunc = () =>
                                {
                                    return ($"= {Simulator.Instance.ActivityRun.Tasks.OfType<ActivityTaskPassengerStopAt>().Where((stopTask) => !stopTask.ActualArrival.HasValue).Count()}", Color.White);
                                };
                                (string text, Color textColor) remainingStops = remainingStopsFunc();
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Station Stops remaining:", remainingStops.text, remainingStops.textColor, 20);
                                activityEvaluation.Add((functionLabel, remainingStopsFunc));
                            }
                            
                            int taskCount = Simulator.Instance.ActivityRun.EventList.Select((wrapper) => wrapper.ActivityEvent).OfType<ActionActivityEvent>()
                                .Where((activityTask) => activityTask.Type != EventType.AllStops && activityTask.Type != EventType.ReachSpeed).Count();
                            if (taskCount > 0)
                            {
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Tasks:", $"= {taskCount}", 20);

                                Func<(string text, Color textColor)> taskDoneFunc = () =>
                                {
                                    int count = Simulator.Instance.ActivityRun.EventList.Where((wrapper) => wrapper.TimesTriggered == 1).Select((wrapper) => wrapper.ActivityEvent).OfType<ActionActivityEvent>().
                                        Where((activityTask) => activityTask.Type != EventType.AllStops && activityTask.Type != EventType.ReachSpeed).Count();
                                    return ($"= {count}", Color.White);
                                };
                                (string text, Color textColor) taskDone = taskDoneFunc();
                                functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Tasks accomplished:", taskDone.text, 20);
                                activityEvaluation.Add((functionLabel, taskDoneFunc));
                            }
                            functionLabel = AddEvaluationLine(evaluationLayoutContainer, "Train Overturned", $"= {ActivityEvaluation.Instance.TrainOverTurned}");
                            activityEvaluation.Add((functionLabel, () => ($"= {ActivityEvaluation.Instance.TrainOverTurned}", Color.White)));

                        };
                        (evaluationTab as TabControl<EvaluationTabSettings>).TabLayouts[EvaluationTabSettings.Report] = (evaluationLayoutContainer) =>
                        {
                            reportText = new TextBox(this, 0, 0, evaluationLayoutContainer.RemainingWidth, evaluationLayoutContainer.RemainingHeight, 
                                "Report will be available here when activity is completed.", HorizontalAlignment.Left, false, Owner.TextFontMonoDefault, Color.White);
                            evaluationLayoutContainer.Add(reportText);
                        };
                        layoutContainer.Add(evaluationTab);
                    };
                }
                #endregion
            }
            else if (Simulator.Instance.TimetableMode)
            {
                tabControl.TabLayouts[TabSettings.TimetableBriefing] = (layoutContainer) =>
                {
                    TextBox timetableBriefing = new TextBox(this, layoutContainer.RemainingWidth, layoutContainer.RemainingHeight, (viewer.SelectedTrain as TTTrain)?.Briefing, false);
                    layoutContainer.Add(timetableBriefing);
                };
            }
            tabControl.TabLayouts[TabSettings.LocomotiveProcedures] = (layoutContainer) =>
            {
                TextBox proceduresText = new TextBox(this, layoutContainer.RemainingWidth, layoutContainer.RemainingHeight, (Simulator.Instance.PlayerLocomotive as MSTSLocomotive)?.EngineOperatingProcedures, false);
                layoutContainer.Add(proceduresText);
            };
            layout.Add(tabControl);
            return layout;
        }

        private void TabControl_TabChanged(object sender, TabChangedEventArgs<TabSettings> e)
        {
            settings.PopupSettings[ViewerWindowType.HelpWindow] = e.Tab.ToString();
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (EnumExtension.GetValue(settings.PopupSettings[ViewerWindowType.HelpWindow], out TabSettings tab))
                tabControl.TabAction(tab);
            tabControl.TabChanged += TabControl_TabChanged;
        }

        protected override void Update(GameTime gameTime, bool shouldUpdate)
        {
            if (shouldUpdate && ! evaluationCompleted)
            {
                if (Simulator.Instance.ActivityRun != null)
                {
                    if (tabControl.CurrentTab == TabSettings.ActivityTimetable && activityTimetableScrollbox != null &&
                        lastActivityTask != Simulator.Instance.ActivityRun.ActivityTask || stoppedAt != (lastActivityTask is ActivityTaskPassengerStopAt preTest && preTest.ActualArrival != null))
                    {
                        lastActivityTask = Simulator.Instance.ActivityRun.ActivityTask;
                        stoppedAt = (lastActivityTask is ActivityTaskPassengerStopAt stopAtTask && stopAtTask.ActualArrival != null);
                        foreach (ControlLayout line in activityTimetableScrollbox.Controls)
                        {
                            (ActivityTaskPassengerStopAt activityTask, Label actualArrival, Label actualDeparture) = ((ActivityTaskPassengerStopAt, Label, Label))line.Tag;
                            actualArrival.Text = $"{(activityTask.ActualArrival.HasValue ? activityTask.ActualArrival : activityTask.IsCompleted.HasValue && activityTask.NextTask != null ? Catalog.GetString("(missed)") : string.Empty)}";
                            actualArrival.TextColor = ColorCoding.ArrivalColor(activityTask.ScheduledArrival, activityTask.ActualArrival);
                            actualDeparture.Text = $"{(activityTask.ActualDeparture.HasValue ? activityTask.ActualDeparture : activityTask.IsCompleted.HasValue && activityTask.NextTask != null ? Catalog.GetString("(missed)") : string.Empty)}";
                            actualDeparture.TextColor = ColorCoding.DepartureColor(activityTask.ScheduledDeparture, activityTask.ActualDeparture);
                        }
                    }
                    else if (tabControl.CurrentTab == TabSettings.ActivityWorkOrders && activityWorkOrderScrollbox != null && Simulator.Instance.ActivityRun.EventList != null)
                    {
                        if (Simulator.Instance.ActivityRun.LastTriggeredActivityEvent != null && (lastLastEventID == -1 ||
                            (Simulator.Instance.ActivityRun.LastTriggeredActivityEvent.ActivityEvent.ID != lastLastEventID)))
                        {
                            lastLastEventID = Simulator.Instance.ActivityRun.LastTriggeredActivityEvent.ActivityEvent.ID;
                            foreach ((EventWrapper activityEvent, Label label) in activityWorkOrderScrollbox.Tag as List<(EventWrapper, Label)>)
                            {
                                label.Text = activityEvent.TimesTriggered == 1 ? "Done" : string.Empty;
                            }
                        }
                    }
                    else if (tabControl.CurrentTab == TabSettings.ActivityEvaluation)
                    {
                        foreach ((Label label, Func<(string, Color)> func) in evaluationTab.Tag as List<(Label, Func<(string, Color)>)>)
                        {
                            (string text, Color textColor) = func();
                            label.Text = text;
                            label.TextColor = textColor;
                        }
                        if (ActivityEvaluation.Instance.ActivityCompleted && ActivityEvaluation.Instance.ReportText != null)
                        {
                            reportText.SetText(ActivityEvaluation.Instance.ReportText);
                            evaluationCompleted = true;
                            SystemInfo.OpenFile(ActivityEvaluation.Instance.ReportFileName);
                        }
                    }
                }
            }
            base.Update(gameTime, shouldUpdate);
        }

        public override bool Open()
        {
            userCommandController.AddEvent(UserCommand.DisplayHelpWindow, KeyEventType.KeyPressed, TabAction, true);
            return base.Open();
        }

        public override bool Close()
        {
            userCommandController.RemoveEvent(UserCommand.DisplayHelpWindow, KeyEventType.KeyPressed, TabAction);
            return base.Close();
        }

        private void TabAction(UserCommandArgs args)
        {
            if (args is ModifiableKeyCommandArgs keyCommandArgs && (keyCommandArgs.AdditionalModifiers & settings.Input.WindowTabCommandModifier) == settings.Input.WindowTabCommandModifier)
            {
                tabControl?.TabAction();
            }
        }

        private Label AddEvaluationLine(ControlLayout container, string text, string value, int padLeft = 0)
        {
            Label result = null;
            ControlLayout line = container.AddLayoutHorizontalLineOfText();
            if (padLeft > 0)
                line.AddSpace(padLeft, line.RemainingHeight);
            line.Add(new Label(this, container.RemainingWidth / 3 * 2 - padLeft, line.RemainingHeight, text));
            if (!string.IsNullOrEmpty(value))
            {
                line.Add(result = new Label(this, container.RemainingWidth / 3, line.RemainingHeight, value));
            }
            return result;
        }

        private Label AddEvaluationLine(ControlLayout container, string text, string value, Color textColor, int padLeft = 0)
        {
            Label result = null;
            ControlLayout line = container.AddLayoutHorizontalLineOfText();
            if (padLeft > 0)
                line.AddSpace(padLeft, line.RemainingHeight);
            line.Add(new Label(this, container.RemainingWidth / 3 * 2 - padLeft, line.RemainingHeight, text));
            if (!string.IsNullOrEmpty(value))
            {
                line.Add(result = new Label(this, container.RemainingWidth / 3, line.RemainingHeight, value) { TextColor = textColor });
            }
            return result;
        }
    }
}
