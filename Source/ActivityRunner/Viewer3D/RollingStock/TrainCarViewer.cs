﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Simulation.RollingStocks;
using Orts.Common;
using System.Diagnostics;

namespace Orts.ActivityRunner.Viewer3D.RollingStock
{
    public abstract class TrainCarViewer
    {
        // TODO add view location and limits
        public TrainCar Car;
        public LightViewer lightDrawer;

        protected Viewer Viewer;

        public TrainCarViewer(Viewer viewer, TrainCar car)
        {
            Car = car;
            Viewer = viewer;

        }

        public abstract void HandleUserInput(in ElapsedTime elapsedTime);

        public abstract void InitializeUserInputCommands();

        public abstract void RegisterUserCommandHandling();

        public abstract void UnregisterUserCommandHandling();

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public abstract void PrepareFrame(RenderFrame frame, in ElapsedTime elapsedTime);

        public virtual void Unload() { }

        internal virtual void LoadForPlayer() { }

        internal abstract void Mark();


        public float[] Velocity = new float[] { 0, 0, 0 };

        public void UpdateSoundPosition()
        {
            if (Car.SoundSourceIDs.Count == 0 || Viewer.Camera == null)
                return;

            if (Car.Train != null)
            {
                var realSpeedMpS = Car.SpeedMpS;
                //TODO Following if block is needed due to physics flipping when using rear cab
                // If such physics flipping is removed next block has to be removed.
                if (Car is MSTSLocomotive)
                {
                    var loco = Car as MSTSLocomotive;
                    if (loco.UsingRearCab) realSpeedMpS = -realSpeedMpS;
                }
                Vector3 directionVector = Vector3.Multiply(Car.WorldPosition.XNAMatrix.Forward, realSpeedMpS);
                Velocity = new float[] { directionVector.X, directionVector.Y, -directionVector.Z };
            }
            else
                Velocity = new float[] { 0, 0, 0 };

            float[] soundLocation = new float[3];
            // TODO This entire block of code (down to TODO END) should be inside the SoundProcess, not here.
            Car.WorldPosition.WorldLocation.NormalizeTo(Camera.SoundBaseTile.X, Camera.SoundBaseTile.Y).Location.Deconstruct(out soundLocation[0], out soundLocation[1], out soundLocation[2]);

            // make a copy of SoundSourceIDs, but check that it didn't change during the copy; if it changed, try again up to 5 times.
            var sSIDsFinalCount = -1;
            var sSIDsInitCount = -2;
            int[] soundSourceIDs = { 0 };
            int trialCount = 0;
            try
            {
                while (sSIDsInitCount != sSIDsFinalCount && trialCount < 5)
                {
                    sSIDsInitCount = Car.SoundSourceIDs.Count;
                    soundSourceIDs = Car.SoundSourceIDs.ToArray();
                    sSIDsFinalCount = Car.SoundSourceIDs.Count;
                    trialCount++;
                }
            }
            catch
            {
                Trace.TraceInformation("Skipped update of position and speed of sound sources");
                return;
            }
            if (trialCount >= 5)
                return;
            foreach (var soundSourceID in soundSourceIDs)
            {
                Viewer.Simulator.UpdaterWorking = true;
                if (OpenAL.IsSource(soundSourceID))
                {
                    OpenAL.Sourcefv(soundSourceID, OpenAL.AL_POSITION, soundLocation);
                    OpenAL.Sourcefv(soundSourceID, OpenAL.AL_VELOCITY, Velocity);
                }
                Viewer.Simulator.UpdaterWorking = false;
            }
            // TODO END
        }
    }
}
