﻿// COPYRIGHT 2018 by the Open Rails project.
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

using System.IO;

using Orts.Formats.Msts.Files;

namespace Orts.ContentChecker
{
    /// <summary>
    /// Loader class for .eng files
    /// </summary>
    internal sealed class SignalConfigLoader : Loader
    {
        private SignalConfigurationFile sigcfg;

        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            string subdirname = Path.GetFileName(Path.GetDirectoryName(file));
            sigcfg = new SignalConfigurationFile(file, subdirname.Equals("openrails", System.StringComparison.OrdinalIgnoreCase));
        }

        protected override void AddDependentFiles()
        {
            foreach (string scriptfile in sigcfg.ScriptFiles)
            {
                Loader scriptLoader = new SignalScriptLoader(sigcfg);
                string scriptfileFull = Path.Combine(sigcfg.ScriptPath, scriptfile);
                AddAdditionalFileAction.Invoke(scriptfileFull, scriptLoader);
            }
        }
    }
}
