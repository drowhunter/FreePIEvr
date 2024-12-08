using System;
using System.Collections.Generic;
using System.Linq;


namespace FreePIE.Core.Plugins.keyboard
{
	
	
	/// <summary>
	/// Description of key.
	/// </summary>
	public sealed class key //: KeyBase //, IEquatable<key>
	{
        private static List<key> _keys => KeyCollection.keys;
		
		
		public readonly char lower;
		public readonly char upper;
		/// <summary>
		/// the sendinput scancode
		/// </summary>
		public readonly ushort scanCode;
		
        /// <summary>
		/// is this key part of the extened key map
		/// </summary>
		public readonly bool IsExtended;

        private int _value;
        public int value { 
			get { return _value; } 
		}

		public string name { get; private set; }
		public key(int value, string name, ushort scancode) 
            : this(value, name, scancode, false, '\0', '\0') { }

        public key(int value, string name, ushort scancode, char lower) 
            : this(value, name, scancode, false, lower, '\0') { }

        public key(int value, string name, ushort scancode, char lower, char upper) 
            : this(value, name, scancode, false, lower, upper) { }

        public key(int value, string name, ushort scancode, bool isExtended) 
            : this(value, name, scancode, isExtended, '\0', '\0') { }
		//public key(string name,int i, char lower='\0',char upper='\0')
        public key(int value, string name, ushort scancode, bool isExtended, char lower, char upper)
            //: base(value, name)
		{
			this.name = name;	
            this._value = value;
            this.upper = upper;
			this.lower = lower;
			this.scanCode = scancode;
			this.IsExtended = isExtended;
		}

        /// <summary>
        /// return a list of keys neccessary to type a character
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public static key[] FromChar(char character)
	    {
            var retval = new List<key>();

	        //_keys.Where(k => k.lower == character || k.upper == character).SingleOrDefault()
	        var ky = _keys.SingleOrDefault(k => k.lower == character || k.upper == character);
	        if (ky != null)
	        {
	            if (ky.upper == character)
	                retval.Add(key.LeftShift);
                
                retval.Add(ky);

                
	        }

	        return retval.ToArray();
	    }

        /// <summary>
        /// True if this key has an ascii value
        /// </summary>
        public bool hasAscii => lower != '\0';

        public override string ToString()
		{
		    if (lower != '\0')
		        return lower.ToString();
		    else
		        return base.ToString();
		}

		#region Casting and Conversion
		public static implicit operator key(int d)
		{
			try
			{
				return _keys[d];
			}
			catch (Exception ex)
			{
				throw new IndexOutOfRangeException("there is no key at index " + d, ex);
			}

		}

        public static implicit operator int(key d) => d.value;

        public static implicit operator key(SharpDX.DirectInput.Key d) => _keys[(int)d];

        public static implicit operator SharpDX.DirectInput.Key(key d) => (SharpDX.DirectInput.Key) d.scanCode;

		#endregion

		#region Equals and GetHashCode implementation
		// The code in this region is useful if you want to use this structure in collections.
		// If you don't need it, you can just remove the region and the ": IEquatable<Slimkey>" declaration.

		public override bool Equals(object obj)
		{
			if (obj is key)
				return Equals((key)obj); // use Equals method below
			else
				return false;
		}

        //public bool Equals(key other)
        //{
        //    return this.val == other.val;
        //}

		public override int GetHashCode()
		{
			// combine the hash codes of all members here (e.g. with XOR operator ^)
			return value.GetHashCode() ^ upper.GetHashCode() ^ lower.GetHashCode();
		}

		public static bool operator ==(key left, key right)
		{
            if (ReferenceEquals(null, left)) return false;
			return left.Equals(right);
		}

		public static bool operator !=(key left, key right)
		{
            if (ReferenceEquals(null, left)) return false;
			return !left.Equals(right);
		}
		#endregion

		#region Keys

		public static readonly key D0 = _keys[0];// new key(0, "D0", 0x0B, '0', ')');
		public static readonly key D1 = _keys[1];
		public static readonly key D2 = _keys[2];
		public static readonly key D3 = _keys[3];
		public static readonly key D4 = _keys[4];
		public static readonly key D5 = _keys[5];
		public static readonly key D6 = _keys[6];
		public static readonly key D7 = _keys[7];
		public static readonly key D8 = _keys[8];
		public static readonly key D9 = _keys[9];
		public static readonly key A = _keys[10];
		public static readonly key B = _keys[11];
		public static readonly key C = _keys[12];
		public static readonly key D = _keys[13];
		public static readonly key E = _keys[14];
		public static readonly key F = _keys[15];
		public static readonly key G = _keys[16];
		public static readonly key H = _keys[17];
		public static readonly key I = _keys[18];
		public static readonly key J = _keys[19];
		public static readonly key K = _keys[20];
		public static readonly key L = _keys[21];
		public static readonly key M = _keys[22];
		public static readonly key N = _keys[23];
		public static readonly key O = _keys[24];
		public static readonly key P = _keys[25];
		public static readonly key Q = _keys[26];
		public static readonly key R = _keys[27];
		public static readonly key S = _keys[28];
		public static readonly key T = _keys[29];
		public static readonly key U = _keys[30];
		public static readonly key V = _keys[31];
		public static readonly key W = _keys[32];
		public static readonly key X = _keys[33];
		public static readonly key Y = _keys[34];
		public static readonly key Z = _keys[35];
		public static readonly key AbntC1 = _keys[36];//not tested
		public static readonly key AbntC2 = _keys[37];//not tested
		public static readonly key Apostrophe = _keys[38];
		public static readonly key Applications = _keys[39];
		public static readonly key AT = _keys[40];
		public static readonly key AX = _keys[41];
		public static readonly key Backspace = _keys[42];
		public static readonly key Backslash = _keys[43];
		public static readonly key Calculator = _keys[44];
		public static readonly key CapsLock = _keys[45];
		public static readonly key Colon = _keys[46];//not tested
		public static readonly key Comma = _keys[47];
		public static readonly key Convert = _keys[48];//not tested
		public static readonly key Delete = _keys[49];
		public static readonly key DownArrow = _keys[50];
		public static readonly key End = _keys[51];
		public static readonly key equals = _keys[52];
		public static readonly key Escape = _keys[53];
		public static readonly key F1 = _keys[54];
		public static readonly key F2 = _keys[55];
		public static readonly key F3 = _keys[56];
		public static readonly key F4 = _keys[57];
		public static readonly key F5 = _keys[58];
		public static readonly key F6 = _keys[59];
		public static readonly key F7 = _keys[60];
		public static readonly key F8 = _keys[61];
		public static readonly key F9 = _keys[62];
		public static readonly key F10 = _keys[63];
		public static readonly key F11 = _keys[64];
		public static readonly key F12 = _keys[65];
		public static readonly key F13 = _keys[66];
		public static readonly key F14 = _keys[67];
		public static readonly key F15 = _keys[68];
		public static readonly key Grave = _keys[69];
		public static readonly key Home = _keys[70];
		public static readonly key Insert = _keys[71];
		public static readonly key Kana = _keys[72];
		public static readonly key Kanji = _keys[73];
		public static readonly key LeftBracket = _keys[74];
		public static readonly key LeftControl = _keys[75];
		public static readonly key LeftArrow = _keys[76];
		public static readonly key LeftAlt = _keys[77];
		public static readonly key LeftShift = _keys[78];
		public static readonly key LeftWindowsKey = _keys[79];
		public static readonly key Mail = _keys[80];
		public static readonly key MediaSelect = _keys[81];
		public static readonly key MediaStop = _keys[82];
		public static readonly key Minus = _keys[83];
		public static readonly key Mute = _keys[84];
		public static readonly key MyComputer = _keys[85];
		public static readonly key NextTrack = _keys[86];
		public static readonly key NoConvert = _keys[87];//not tested
		public static readonly key NumberLock = _keys[88];
		public static readonly key NumberPad0 = _keys[89];
		public static readonly key NumberPad1 = _keys[90];
		public static readonly key NumberPad2 = _keys[91];
		public static readonly key NumberPad3 = _keys[92];
		public static readonly key NumberPad4 = _keys[93];
		public static readonly key NumberPad5 = _keys[94];
		public static readonly key NumberPad6 = _keys[95];
		public static readonly key NumberPad7 = _keys[96];
		public static readonly key NumberPad8 = _keys[97];
		public static readonly key NumberPad9 = _keys[98];
		public static readonly key NumberPadComma = _keys[99];
		public static readonly key NumberPadEnter = _keys[100];
		public static readonly key NumberPadEquals = _keys[101];
		public static readonly key NumberPadMinus = _keys[102];
		public static readonly key NumberPadPeriod = _keys[103];
		public static readonly key NumberPadPlus = _keys[104];
		public static readonly key NumberPadSlash = _keys[105];
		public static readonly key NumberPadStar = _keys[106];
		public static readonly key Oem102 = _keys[107];
		public static readonly key PageDown = _keys[108];
		public static readonly key PageUp = _keys[109];
		public static readonly key Pause = _keys[110];//buggy
		public static readonly key Period = _keys[111];
		public static readonly key PlayPause = _keys[112];
		public static readonly key Power = _keys[113];//not tested
		public static readonly key PreviousTrack = _keys[114];
		public static readonly key RightBracket = _keys[115];
		public static readonly key RightControl = _keys[116];
		public static readonly key Return = _keys[117];
		public static readonly key RightArrow = _keys[118];
		public static readonly key RightAlt = _keys[119];
		public static readonly key RightShift = _keys[120];
		public static readonly key RightWindowsKey = _keys[121];
		public static readonly key ScrollLock = _keys[122];
		public static readonly key Semicolon = _keys[123];
		public static readonly key Slash = _keys[124];
		public static readonly key Sleep = _keys[125];//not tested
		public static readonly key Space = _keys[126];
		public static readonly key Stop = _keys[127];//not tested
		public static readonly key PrintScreen = _keys[128];
		public static readonly key Tab = _keys[129];
		public static readonly key Underline = _keys[130];//not tested
		public static readonly key Unlabeled = _keys[131];//not tested
		public static readonly key UpArrow = _keys[132];
		public static readonly key VolumeDown = _keys[133];
		public static readonly key VolumeUp = _keys[134];
		public static readonly key Wake = _keys[135]; //not tested
		public static readonly key WebBack = _keys[136];
		public static readonly key WebFavorites = _keys[137];
		public static readonly key WebForward = _keys[138];
		public static readonly key WebHome = _keys[139];
		public static readonly key WebRefresh = _keys[140];
		public static readonly key WebSearch = _keys[141];
		public static readonly key WebStop = _keys[142];
		public static readonly key Yen = _keys[143];      //not tested
		public static readonly key Unknown = _keys[144];     //not tested

		#endregion
	}
}
