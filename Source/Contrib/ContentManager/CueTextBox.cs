// COPYRIGHT 2018 by the Open Rails project.
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
using System.Windows.Forms;
using static Orts.Common.Native.NativeMethods;

namespace Orts.ContentManager
{
    public class CueTextBox : TextBox
    {
        private const int EM_SETCUEBANNER = 0x1501;

        private string cueText;

        public string Cue
        {
            get => cueText;
            set
            {
                cueText = value;
                UpdateCueText();
            }
        }

        public bool ShowCueWhenFocused { get; set; }

        private void UpdateCueText()
        {
            if (IsHandleCreated && !string.IsNullOrEmpty(cueText))
            {
                SendMessage(Handle, EM_SETCUEBANNER, (IntPtr)Convert.ToInt32(ShowCueWhenFocused), cueText);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateCueText();
        }
    }
}