﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using FreeTrainSimulator.Common.Native;

using Microsoft.Xna.Framework.Input;

namespace Orts.Common.Input
{
    public static class ScanCodeKeyUtils
    {
        public static Keys GetScanCodeKeys(int scanCode)
        {
            if (scanCode >= 0x0100)
                scanCode = 0xE100 | (scanCode & 0x7F);
            else if (scanCode >= 0x0080)
                scanCode = 0xE000 | (scanCode & 0x7F);
            return (Keys)NativeMethods.MapVirtualKey(scanCode, NativeMethods.MapVirtualKeyType.ScanToVirtualEx);
        }

        public static string GetScanCodeKeyName(int scanCode)
        {
            string xnaName = Enum.GetName(typeof(Keys), GetScanCodeKeys(scanCode));
            StringBuilder keyNameBuilder = new StringBuilder();
            _ = NativeMethods.GetKeyNameText(scanCode << 16, keyNameBuilder, 256);
            string keyName = keyNameBuilder.ToString();

            if (keyName.Length > 0)
            {
                // Pick the XNA key name because:
                //   Pause (0x11D) is mapped to "Right Control".
                //   GetKeyNameText prefers "NUM 9" to "PAGE UP".
                if (!string.IsNullOrEmpty(xnaName) && ((scanCode == 0x11D) || keyName.StartsWith("NUM ", StringComparison.OrdinalIgnoreCase) || keyName.StartsWith(xnaName, StringComparison.OrdinalIgnoreCase) || xnaName.StartsWith(keyName, StringComparison.OrdinalIgnoreCase)))
                    return xnaName;

                return keyName;
            }

            // If we failed to convert the scan code to a name, show the scan code for debugging.
            return $" [sc=0x{scanCode:X2}]";
        }

    }
    /// <summary>
    /// Represents a single user-triggerable keyboard input command.
    /// </summary>
    public abstract class UserCommandInput
    {
        private protected KeyModifiers modifiers;
        private protected int uniqueIdentifier;

        // one value for each combination in KeyModifiers flags
        private protected static readonly string[] toStringPrefix = { string.Empty, "Shift", "Control", "Shift + Control", "Alt", "Shift + Alt", "Control + Alt", "Shift + Control + Alt" };

        private protected static readonly string[] uniqueInputsPrefix = { string.Empty, "Shift", "Control", "Shift+Control", "Alt", "Shift+Alt", "Control+Alt", "Shift+Control+Alt" };

        protected UserCommandInput(int scanCode, Keys virtualKey, KeyModifiers modifiers)
        {
            uniqueIdentifier = ComposeUniqueDescriptor(modifiers, scanCode, virtualKey);
            this.modifiers = modifiers;
        }

        public abstract int UniqueDescriptor { get; set; }

        public virtual bool IsModifier => false;

        public abstract bool IsKeyDown(KeyboardState keyboardState);

        public abstract IEnumerable<string> GetUniqueInputs();

        public override string ToString()
        {
            return string.Empty;
        }

        public static (bool Shift, bool Control, bool Alt, int ScanCode, int VirtualKey) DecomposeUniqueDescriptor(int uniqueDescriptor)
        {
            byte[] bytes = BitConverter.GetBytes(uniqueDescriptor);
            return (((KeyModifiers)bytes[0] & KeyModifiers.Shift) != 0,
                ((KeyModifiers)bytes[0] & KeyModifiers.Control) != 0,
                ((KeyModifiers)bytes[0] & KeyModifiers.Alt) != 0,
                bytes[1], bytes[2]);
        }

        public static int ComposeUniqueDescriptor(bool shift, bool control, bool alt, int scanCode, Keys virtualKey)
        {
            KeyModifiers modifiers = KeyModifiers.None;
            modifiers = shift ? modifiers | KeyModifiers.Shift : modifiers & ~KeyModifiers.Shift;
            modifiers = control ? modifiers | KeyModifiers.Control : modifiers & ~KeyModifiers.Control;
            modifiers = alt ? modifiers | KeyModifiers.Alt : modifiers & ~KeyModifiers.Alt;

            return ComposeUniqueDescriptor(modifiers, scanCode, virtualKey);
        }

        public static int ComposeUniqueDescriptor(KeyModifiers modifiers, int scanCode, Keys virtualKey)
        {
            return (((byte)virtualKey << 16) | ((byte)scanCode << 8) | ((byte)modifiers << 0));
        }
    }

    /// <summary>
    /// Stores a specific combination of keyboard modifiers for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandModifierInput : UserCommandInput
    {
        public bool Shift => (modifiers & KeyModifiers.Shift) != 0;
        public bool Control => (modifiers & KeyModifiers.Control) != 0;
        public bool Alt => (modifiers & KeyModifiers.Alt) != 0;

        public UserCommandModifierInput(KeyModifiers modifiers) :
            base(0, 0, modifiers)
        {
        }

        protected static bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return (!shift || keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) &&
                (!control || keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                (!alt || keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        }

        public override int UniqueDescriptor
        {
            get => uniqueIdentifier;
            set
            {
                uniqueIdentifier = value;
                byte[] bytes = BitConverter.GetBytes(value);
                modifiers = (KeyModifiers)bytes[0];
            }
        }

        public override bool IsModifier => true;

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            return new[] { uniqueInputsPrefix[(int)modifiers] };
        }

        public override string ToString()
        {
            return toStringPrefix[(int)modifiers];
        }
    }

    /// <summary>
    /// Stores a key and specific combination of keyboard modifiers for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandKeyInput : UserCommandInput
    {
        public int ScanCode { get; private set; }
        public Keys VirtualKey { get; private set; }
        public bool Shift => (modifiers & KeyModifiers.Shift) != 0;
        public bool Control => (modifiers & KeyModifiers.Control) != 0;
        public bool Alt => (modifiers & KeyModifiers.Alt) != 0;

        protected UserCommandKeyInput(int scanCode, Keys virtualKey, KeyModifiers modifiers) :
            base(scanCode, virtualKey, modifiers)
        {
            Debug.Assert((scanCode >= 1 && scanCode <= 127) || (virtualKey != Keys.None), "Scan code for keyboard input is outside the allowed range of 1-127.");
            ScanCode = scanCode;
            VirtualKey = virtualKey;
        }

        public UserCommandKeyInput(int scancode) :
            this(scancode, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(Keys virtualKey) :
            this(virtualKey, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(int scancode, KeyModifiers modifiers) :
            this(scancode, Keys.None, modifiers)
        {
        }

        public UserCommandKeyInput(Keys virtualKey, KeyModifiers modifiers) :
            this(0, virtualKey, modifiers)
        {
        }

        public Keys Key => VirtualKey == Keys.None ? ScanCodeKeyUtils.GetScanCodeKeys(ScanCode) : VirtualKey;

        public KeyModifiers Modifiers => (Alt ? KeyModifiers.Alt : KeyModifiers.None) | (Control ? KeyModifiers.Control : KeyModifiers.None) | (Shift ? KeyModifiers.Shift : KeyModifiers.None);


        protected static bool IsKeyMatching(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        protected static bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return ((keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) == shift) &&
                ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) == control) &&
                ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) == alt);
        }

        public override int UniqueDescriptor
        {
            get => uniqueIdentifier;
            set
            {
                uniqueIdentifier = value;
                byte[] bytes = BitConverter.GetBytes(value);
                modifiers = (KeyModifiers)bytes[0];
                ScanCode = bytes[1];
                VirtualKey = (Keys)bytes[2];
            }
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            return new[] { $"{uniqueInputsPrefix[(int)modifiers]}{(modifiers == KeyModifiers.None ? string.Empty : "+")}{(VirtualKey == Keys.None ? $"0x{ScanCode:X2}" : VirtualKey.ToString())}" };
        }

        public override string ToString()
        {
            return $"{toStringPrefix[(int)modifiers]}{(modifiers == KeyModifiers.None ? string.Empty : " + ")}{(VirtualKey == Keys.None ? ScanCodeKeyUtils.GetScanCodeKeyName(ScanCode) : VirtualKey.ToString())}";
        }
    }

    /// <summary>
    /// Stores a key, specific combination of keyboard modifiers and a set of keyboard modifiers to ignore for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandModifiableKeyInput : UserCommandKeyInput
    {
        private KeyModifiers ignoreModifiers;

        public bool IgnoreShift => (ignoreModifiers & KeyModifiers.Shift) != 0;
        public bool IgnoreControl => (ignoreModifiers & KeyModifiers.Control) != 0;
        public bool IgnoreAlt => (ignoreModifiers & KeyModifiers.Alt) != 0;

        private readonly IEnumerable<UserCommandModifierInput> combine;

        private UserCommandModifiableKeyInput(int scanCode, Keys virtualKey, KeyModifiers modifiers, IEnumerable<UserCommandInput> combine) :
            base(scanCode, virtualKey, modifiers)
        {
            this.combine = combine.Cast<UserCommandModifierInput>();
            SynchronizeCombine();
        }

        public UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, params UserCommandInput[] combine) :
            this(scanCode, Keys.None, modifiers, combine)
        {
        }

        public UserCommandModifiableKeyInput(Keys key, KeyModifiers modifiers, params UserCommandInput[] combine) :
            this(0, key, modifiers, combine)
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, params UserCommandInput[] combine) :
            this(scanCode, KeyModifiers.None, combine)
        {
        }

        public UserCommandModifiableKeyInput(Keys key, params UserCommandInput[] combine) :
            this(key, KeyModifiers.None, combine)
        {
        }

        public override int UniqueDescriptor
        {
            get => uniqueIdentifier;
            set
            {
                base.UniqueDescriptor = value;
                ignoreModifiers = (KeyModifiers)(byte)(value >> 24);
            }
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            bool shiftState = IgnoreShift ? keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift) : Shift;
            bool controlState = IgnoreControl ? keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl) : Control;
            bool altState = IgnoreAlt ? keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt) : Alt;
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, shiftState, controlState, altState);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            IEnumerable<string> inputs = new[] { Key.ToString() };

            // This must result in the output being Shift+Control+Alt+key.

            if (IgnoreAlt)
                inputs = inputs.SelectMany(i => new[] { i, "Alt+" + i });
            else if (Alt)
                inputs = inputs.Select(i => "Alt+" + i);

            if (IgnoreControl)
                inputs = inputs.SelectMany(i => new[] { i, "Control+" + i });
            else if (Control)
                inputs = inputs.Select(i => "Control+" + i);

            if (IgnoreShift)
                inputs = inputs.SelectMany(i => new[] { i, "Shift+" + i });
            else if (Shift)
                inputs = inputs.Select(i => "Shift+" + i);

            return inputs;
        }

        public override string ToString()
        {
            StringBuilder key = new StringBuilder(base.ToString());
            if (IgnoreShift) key.Append(" (+ Shift)");
            if (IgnoreControl) key.Append(" (+ Control)");
            if (IgnoreAlt) key.Append(" (+ Alt)");
            return key.ToString();
        }

        public void SynchronizeCombine()
        {
            ignoreModifiers = combine.Any(c => c.Shift) ? ignoreModifiers | KeyModifiers.Shift : ignoreModifiers & ~KeyModifiers.Shift;
            ignoreModifiers = combine.Any(c => c.Control) ? ignoreModifiers | KeyModifiers.Control : ignoreModifiers & ~KeyModifiers.Control;
            ignoreModifiers = combine.Any(c => c.Alt) ? ignoreModifiers | KeyModifiers.Alt : ignoreModifiers & ~KeyModifiers.Alt;
            uniqueIdentifier |= ((byte)ignoreModifiers << 24);
        }
    }
}
