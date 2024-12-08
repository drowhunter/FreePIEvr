using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FreePIE.Core.Plugins.keyboard
{
    //
    public sealed class KeyCollection // : SingletonBase<KeyCollection>, IEnumerable<key>
    {
        public static readonly List<key> keys =

        //public KeyCollection()
        //{

             new List<key> {
                new key(0, "D0", 0x0B, '0', ')'),
                new key(1, "D1", 0x02, '1', '!'),
                new key(2, "D2", 0x03, '2', '@'),
                new key(3, "D3", 0x04, '3', '#'),
                new key(4, "D4", 0x05, '4', '$'),
                new key(5, "D5", 0x06, '5', '%'),
                new key(6, "D6", 0x07, '6', '^'),
                new key(7, "D7", 0x08, '7', '&'),
                new key(8, "D8", 0x09, '8', '*'),
                new key(9, "D9", 0x0A, '9', '('),

                new key(10, "A", 0x1E, 'a', 'A'),
                new key(11, "B", 0x30, 'b', 'B'),
                new key(12, "C", 0x2E, 'c', 'C'),
                new key(13, "D", 0x20, 'd', 'D'),
                new key(14, "E", 0x12, 'e', 'E'),
                new key(15, "F", 0x21, 'f', 'F'),
                new key(16, "G", 0x22, 'g', 'G'),
                new key(17, "H", 0x23, 'h', 'H'),
                new key(18, "I", 0x17, 'i', 'I'),
                new key(19, "J", 0x24, 'j', 'J'),
                new key(20, "K", 0x25, 'k', 'K'),
                new key(21, "L", 0x26, 'l', 'L'),
                new key(22, "M", 0x32, 'm', 'M'),
                new key(23, "N", 0x31, 'n', 'N'),
                new key(24, "O", 0x18, 'o', 'O'),
                new key(25, "P", 0x19, 'p', 'P'),
                new key(26, "Q", 0x10, 'q', 'Q'),
                new key(27, "R", 0x13, 'r', 'R'),
                new key(28, "S", 0x1F, 's', 'S'),
                new key(29, "T", 0x14, 't', 'T'),
                new key(30, "U", 0x16, 'u', 'U'),
                new key(31, "V", 0x2F, 'v', 'V'),
                new key(32, "W", 0x11, 'w', 'W'),
                new key(33, "X", 0x2D, 'x', 'X'),
                new key(34, "Y", 0x15, 'y', 'Y'),
                new key(35, "Z", 0x2C, 'z', 'Z'),

                new key(36, "AbntC1", 0x73),//not tested
                new key(37, "AbntC2", 0x7E),//not tested
                new key(38, "Apostrophe", 0x28, '\'', '"'),
                new key(39, "Applications", 0xDD, true),
                new key(40, "AT", 0x91),
                new key(41, "AX", 0x96),
                new key(42, "Backspace", 0x0E),
                new key(43, "Backslash", 0x2B, '\\', '|'),
                new key(44, "Calculator", 0xA1, true),
                new key(45, "CapsLock", 0x3A),
                new key(46, "Colon", 0x92, true),//not tested
                new key(47, "Comma", 0x33, ',', '<'),
                new key(48, "Convert", 0x79, true),//not tested
                new key(49, "Delete", 0xD3, true),
                new key(50, "DownArrow", 0xD0, true),
                new key(51, "End", 0xCF, true),
                new key(52, "equals", 0x0D, '=', '+'),
                new key(53, "Escape", 0x01, '`', '~'),

                new key(54, "F1", 0x3B),
                new key(55, "F2", 0x3C),
                new key(56, "F3", 0x3D),
                new key(57, "F4", 0x3E),
                new key(58, "F5", 0x3F),
                new key(59, "F6", 0x40),
                new key(60, "F7", 0x41),
                new key(61, "F8", 0x42),
                new key(62, "F9", 0x43),
                new key(63, "F10", 0x44),
                new key(64, "F11", 0x57),
                new key(65, "F12", 0x58),
                new key(66, "F13", 0x64),
                new key(67, "F14", 0x65),
                new key(68, "F15", 0x66),

                new key(69, "Grave", 0x29, '`', '~'),
                new key(70, "Home", 0xC7, true),
                new key(71, "Insert", 0xD2, true),

                new key(72, "Kana", 0x70),
                new key(73, "Kanji", 0x94),
                new key(74, "LeftBracket", 0x1A, '[', '{'),
                new key(75, "LeftControl", 0x1D),
                new key(76, "LeftArrow", 0xCB, true),
                new key(77, "LeftAlt", 0x38),
                new key(78, "LeftShift", 0x2A),
                new key(79, "LeftWindowsKey", 0xDB, true),
                new key(80, "Mail", 0xEC, true),

                new key(81, "MediaSelect", 0xED, true),
                new key(82, "MediaStop", 0xA4, true),
                new key(83, "Minus", 0x0C, '-', '_'),
                new key(84, "Mute", 0xA0, true),
                new key(85, "MyComputer", 0xEB, true),
                new key(86, "NextTrack", 0x99, true),
                new key(87, "NoConvert", 0x7B),//not tested

                new key(88, "NumberLock", 0x45),
                new key(89, "NumberPad0", 0x52),
                new key(90, "NumberPad1", 0x4F),
                new key(91, "NumberPad2", 0x50),
                new key(92, "NumberPad3", 0x51),
                new key(93, "NumberPad4", 0x4B),
                new key(94, "NumberPad5", 0x4C),
                new key(95, "NumberPad6", 0x4D),
                new key(96, "NumberPad7", 0x47),
                new key(97, "NumberPad8", 0x48),
                new key(98, "NumberPad9", 0x49),

                new key(99, "NumberPadComma", 0xB3),
                new key(100, "NumberPadEnter", 0x9C, true),
                new key(101, "NumberPadEquals", 0x8D),
                new key(102, "NumberPadMinus", 0x4A),
                new key(103, "NumberPadPeriod", 0x53),
                new key(104, "NumberPadPlus", 0x4E),
                new key(105, "NumberPadSlash", 0xB5, true),
                new key(106, "NumberPadStar", 0x37),

                new key(107, "Oem102", 0x56),
                new key(108, "PageDown", 0xD1, true),
                new key(109, "PageUp", 0xC9, true),
                new key(110, "Pause", 0xC5, true),//buggy

                new key(111, "Period", 0x34, '.', '>'),
                new key(112, "PlayPause", 0xA2, true),
                new key(113, "Power", 0xDE, true),//not tested
                new key(114, "PreviousTrack", 0x90, true),
                new key(115, "RightBracket", 0x1B, ']', '}'),
                new key(116, "RightControl", 0x9D, true),
                new key(117, "Return", 0x1C, '\n'),
                new key(118, "RightArrow", 0xCD, true),
                new key(119, "RightAlt", 0xB8, true),
                new key(120, "RightShift", 0x36),

                new key(121, "RightWindowsKey", 0xDC, true),
                new key(122, "ScrollLock", 0x46),
                new key(123, "Semicolon", 0x27, ';', ':'),
                new key(124, "Slash", 0x35, '/', '?'),
                new key(125, "Sleep", 0xDF, true),//not tested
                new key(126, "Space", 0x39, ' '),
                new key(127, "Stop", 0x95, true),//not tested
                new key(128, "PrintScreen", 0xB7, true),
                new key(129, "Tab", 0x0F, '\t'),
                new key(130, "Underline", 0x93),//not tested

                new key(131, "Unlabeled", 0x97),//not tested
                new key(132, "UpArrow", 0xC8, true),
                new key(133, "VolumeDown", 0xAE, true),
                new key(134, "VolumeUp", 0xB0, true),
                new key(135, "Wake", 0xE3, true), //not tested
                new key(136, "WebBack", 0xEA, true),
                new key(137, "WebFavorites", 0xE6, true),
                new key(138, "WebForward", 0xE9, true),
                new key(139, "WebHome", 0xB2, true),
                new key(140, "WebRefresh", 0xE7, true),

                new key(141, "WebSearch", 0xE5, true),
                new key(142, "WebStop", 0xE8, true),
                new key(143, "Yen", 0x7D, true),      //not tested
                new key(144, "Unknown", 0)     //not tested
            };
        //}

        //public IEnumerator<key> GetEnumerator()
        //{
        //    return keys.GetEnumerator();
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return keys.GetEnumerator();
        //}

        //public key this[int index]
        //{
        //    get { return keys[index]; }
        //    set { keys[index] = value; }
        //}
    }
}
