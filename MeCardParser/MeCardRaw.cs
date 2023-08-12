using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Windows.Devices.Bluetooth.Advertisement;

namespace MeCardParser
{
    public class MeCardRawWiFi
    {
        public enum Validity
        {
            // Copy the Validity enum from MeCardRaw
            Valid, InvalidOther, InvalidNull, InvalidLength, InvalidNoScheme, // can't even try to parse
            InvalidEndSemicolons, InvalidColon, // these are "low level details are wrong"
            InvalidHigherLevel, // is valid as Raw, but not as the higher level scheme

            InvalidWrongScheme = 100, // not even a WIFI or WIFISETUP scheme
            InvalidNotUrlEncoded, InvalidNotUnreserved, InvalidNotHex, InvalidNotTrue, InvalidNotBase64, // these are "low level value details are wrong"
            InvalidNoSsid, InvalidOpcodeDuplicate, InvalidOpcodeOrder // these are all high-level opcode issues
        };
        public static void ValidateAsWiFi(MeCardRaw retval)
        {
            bool errorIsLowLevel = false;

            if (retval.Scheme != null && retval.SchemeCanonical != "WIFI" && retval.SchemeCanonical != "WIFISETUP")
            {
                // Why is scheme tested before basic low-level stuff?
                // Because if someone passes in http://example.com I should return "wrong scheme" and not "doesn't end in semi colons"
                // Source: saw it, and decided it was wrong.
                retval.IsValidWiFi = Validity.InvalidWrongScheme;
                retval.ErrorMessage = "WiFi URL doesn't start with WIFI:";
            }
            else if (retval.IsValid != MeCardRaw.Validity.Valid)
            {
                // Can't even try to parse this; just give up. Copy the original
                errorIsLowLevel = true;
                retval.IsValidWiFi = (Validity)retval.IsValid;
            }
            else
            {
            }
            ValidateAsWiFiDuplicates(retval);
            // 2023-07-20: don't check this: ValidateAsWiFiOrder(retval);
            // Per user report: Samsung phones will generate invalid wifi: URLS
            // like wifi:S:iPhone;T:WPA;P:1111;H:false;; 
            // the order in the Samsung is STPH but the correct order is TSHP

            foreach (var index in retval.Fields)
            {
                if (retval.IsValidWiFi == Validity.Valid)
                {
                    ValidateAsWiFiField(retval, index.Value);
                }
            }
            if (retval.IsValidWiFi == Validity.Valid)
            {
                if (!retval.Fields.ContainsKey("S"))  // No SSID is an error
                {
                    retval.IsValidWiFi = Validity.InvalidNoSsid;
                    retval.ErrorMessage = "WiFi URL does not include a required S:ssid value";
                }
            }


            if (retval.IsValidWiFi == Validity.Valid)
            {
                // Is all good; set as valid (defaults are set elsewhere)
                retval.IsValidWiFi = MeCardRawWiFi.Validity.Valid;
            }
            else
            {
                // Clean up error values as needed.
                if (!errorIsLowLevel) // if the error is low level, keep it the way it is.
                {
                    retval.IsValid = MeCardRaw.Validity.InvalidHigherLevel;
                }
            }
        }

        public static void WiFiFillDefaults(MeCardRaw retval)
        {
            if (retval.IsValidWiFi != Validity.Valid) return;  // Always return early if error already detected.

            if (!retval.Fields.ContainsKey("ACTION"))
            {
                switch (retval.SchemeCanonical)
                {
                    case "WIFI": retval.AddField(new MeCardRawField("ACTION", "CONNECT")); break;
                    case "WIFISETUP": retval.AddField(new MeCardRawField("ACTION", "SETUP")); break; //TODO: CREATE would be better name
                }
            }
        }
        static Dictionary<string, int> DefinedWiFiOpcodes = new Dictionary<string, int>()
            {
                { "T", 0 },
                { "R", 1 },
                { "S", 2 },
                { "H", 3 },
                { "I", 4 },
                { "P", 5 },
                { "K", 6 },
            };
        private static void ValidateAsWiFiOrder(MeCardRaw retval)
        {
            if (retval.IsValidWiFi != Validity.Valid) return;  // Always return early if error already detected.

            int lastSeen = -1;
            foreach (var field in retval.FieldsInOrder)
            {
                if (DefinedWiFiOpcodes.ContainsKey(field.Opcode))
                {
                    var currorder = DefinedWiFiOpcodes[field.Opcode];
                    if (currorder < lastSeen)
                    {
                        // Out of order.
                        retval.IsValidWiFi = Validity.InvalidOpcodeOrder;
                        retval.ErrorMessage = "WiFi URL is not in correct order (T, R, S, H I, P, K)";
                    }
                    lastSeen = currorder;
                }
            }
        }
        private static void ValidateAsWiFiDuplicates(MeCardRaw retval)
        {
            if (retval.IsValidWiFi != Validity.Valid) return; // Always return early if error already detected.

            HashSet<string> seenOpcodes = new HashSet<string>();

            foreach (var field in retval.FieldsInOrder)
            {
                if (DefinedWiFiOpcodes.ContainsKey(field.Opcode))
                {
                    if (seenOpcodes.Contains(field.Opcode))
                    {
                        retval.IsValidWiFi = Validity.InvalidOpcodeDuplicate;
                        retval.ErrorMessage = "WiFi URL contains a duplicate opcode";
                    }
                    else
                    {
                        seenOpcodes.Add(field.Opcode);
                    }
                }
            }
        }

        private static void ValidateAsWiFiField(MeCardRaw retval, MeCardRawField field)
        {
            if (retval.IsValidWiFi != Validity.Valid) return; // Always return early if error already detected.

            string decoded;
            switch (field.Opcode)
            {
                case "H":
                    // 2023-08-12: Android is setting H to "false"
                    int strictLevel = 2; // 0=true 1=true/false 2=anything
                    bool fieldIsTrue = field.Value == "true";
                    bool fieldIsFalse = field.Value == "false";
                    switch (strictLevel)
                    {
                        case 0:
                            if (!fieldIsTrue)
                            {
                                retval.IsValidWiFi = Validity.InvalidNotTrue;
                                retval.ErrorMessage = $"Item type {field.Opcode} can only be true";
                                return;
                            }
                            break;
                        case 1:
                            if (!(fieldIsTrue || fieldIsFalse))
                            {
                                retval.IsValidWiFi = Validity.InvalidNotTrue;
                                retval.ErrorMessage = $"Item type {field.Opcode} can only be true or false";
                                return;
                            }
                            break;
                        case 2: // accept anything
                            break;
                    }
                    break;
                case "I":
                    if (!field.Value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    decoded = HttpUtility.UrlDecode(field.Value);
                    if (decoded == null)
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    break;
                case "K":
                    if (!field.Value.IsPKChar())
                    {
                        retval.IsValidWiFi = Validity.InvalidNotBase64;
                        retval.ErrorMessage = $"Item type {field.Opcode} has not bsase 64";
                        return;
                    }
                    break;
                case "P": // Password
                    if (!field.Value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    decoded = HttpUtility.UrlDecode(field.Value);
                    if (decoded == null)
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    break;
                case "R":
                    if (!field.Value.IsHexDigit())
                    {
                        retval.IsValidWiFi = Validity.InvalidNotHex;
                        retval.ErrorMessage = $"Item type {field.Opcode} has incorrect non-hex chars";
                        return;
                    }
                    break;
                case "S": // SSID
                    if (!field.Value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    decoded = HttpUtility.UrlDecode(field.Value);
                    if (decoded == null)
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUrlEncoded;
                        retval.ErrorMessage = $"Item type {field.Opcode} was not URL encoded";
                        return;
                    }
                    break;
                case "T": // Type
                    if (!field.Value.IsUnreserved())
                    {
                        retval.IsValidWiFi = Validity.InvalidNotUnreserved;
                        retval.ErrorMessage = $"Item type {field.Opcode} has non-unreserved characters";
                        return;
                    }
                    break;

            }
        }

    }

    /// <summary>
    /// MeCardRaw is the "raw" data from a MeCard parser. This is without any understanding of the fields; it's just the name of the fields and the data
    /// </summary>
    public class MeCardRaw : IEquatable<MeCardRaw>
    {
        public MeCardRaw()
        {

        }
        public MeCardRaw(string scheme, string fieldName, string fieldValue)
        {
            this.IsValid = Validity.Valid;
            this.Scheme = scheme;
            SchemeSeperator = ":";
            var field = new MeCardRawField(fieldName, fieldValue);
            AddField(field); // Always use this method
            Terminator = ";";
        }
        public enum Validity
        {
            Valid, InvalidOther, InvalidNull, InvalidLength, InvalidNoScheme, // can't even try to parse
            InvalidEndSemicolons, InvalidColon, // these are "low level details are wrong"
            InvalidHigherLevel, // is valid as Raw, but not as the higher level scheme
        };


        public Validity IsValid;
        public MeCardRawWiFi.Validity IsValidWiFi;
        public string ErrorMessage;
        /// <summary>
        /// The WIFI in WIFI:S:MySsid;; Does not include the colon (that's SchemeSeperator)
        /// </summary>
        public string Scheme { get; set; } // e.g.; WIFI (no colon)
        public string SchemeCanonical { get { return Scheme.ToUpperInvariant(); } }
        /// <summary>
        /// The first ":" in WIFI:S:myssid;;
        /// </summary>
        public string SchemeSeperator { get; set; } // is always ":"
        /// <summary>
        /// All the fields organized by opcode. Can't have duplicate opcodes; last opcode wins. Add with AddField()
        /// </summary>
        public Dictionary<string, MeCardRawField> Fields { get; } = new Dictionary<string, MeCardRawField>();
        /// <summary>
        /// List of all the fields; happily includes duplicate opcodes. Often used to validate a parsed card.Add with AddField()
        /// </summary>
        public List<MeCardRawField> FieldsInOrder { get;  } = new List<MeCardRawField>();
        public void AddField(MeCardRawField field)
        {
            Fields[field.Opcode] = field;
            FieldsInOrder.Add(field);
        }
        public MeCardRawField GetField(string opcode)
        {
            MeCardRawField retval = null;
            Fields.TryGetValue(opcode, out retval);
            return retval;
        }
        public string GetFieldValue(string opcode, string defaultValue)
        {
            MeCardRawField field = null;
            var ok = Fields.TryGetValue(opcode, out field);
            var retval = ok ? field.Value : defaultValue;
            return retval;
        }
        /// <summary>
        /// Returns the first index, last index, and count of fields that match the opcode.
        /// </summary>
        public (int, int, int) FieldIndexDataCount(string opcode)
        {
            int count = 0;
            int firstIndex = -1;
            int lastIndex = -1;
            for (int i = 0; i<FieldsInOrder.Count; i++)
            {
                var field = FieldsInOrder[i];
                if (field.Opcode == opcode)
                {
                    if (firstIndex == -1) firstIndex = i;
                    lastIndex = i;
                    count++;
                }
            }
            return (firstIndex, lastIndex, count);
        }
        /// <summary>
        /// The final ";" in WIFI:S:MySsid;;
        /// </summary>
        public string Terminator { get; set; } // is always ";"

        public override string ToString()
        {
            var fields = "";
            foreach (var field in Fields)
            {
                fields += field.Value.ToString();
            }
            var retval = $"{Scheme}{SchemeSeperator}{fields}{Terminator}";
            return retval;
        }

        #region EQUALITY_OVERLOADS
        public override bool Equals(object obj)
        {
            return Equals(obj as MeCardRaw);
        }

        public bool Equals(MeCardRaw other)
        {
            // FieldsInOrder always exists.
            var fieldsEqual = FieldsInOrder.Count == other.FieldsInOrder.Count;
            if (fieldsEqual)
            {
                for (int i = 0; i < FieldsInOrder.Count; i++)
                {
                    fieldsEqual = fieldsEqual && FieldsInOrder[i] == other.FieldsInOrder[i];
                }
            }
            return !(other is null) &&
                   IsValid == other.IsValid &&
                   SchemeCanonical == other.SchemeCanonical &&
                   fieldsEqual;
        }

        public override int GetHashCode()
        {
            int hashCode = 2103109722;
            hashCode = hashCode * -1521134295 + IsValid.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SchemeCanonical);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<MeCardRawField>>.Default.GetHashCode(FieldsInOrder);
            return hashCode;
        }

        public static bool operator ==(MeCardRaw left, MeCardRaw right)
        {
            return EqualityComparer<MeCardRaw>.Default.Equals(left, right);
        }

        public static bool operator !=(MeCardRaw left, MeCardRaw right)
        {
            return !(left == right);
        }

        #endregion


    }

    public class MeCardRawField : IEquatable<MeCardRawField>
    {
        public MeCardRawField(string opcode, string value)
        {
            Opcode = opcode;
            Seperator = ":";
            Value = value;
            Terminator = ";";
        }
        public string Opcode { get; set; }
        public string Seperator { get; set; } // will always be ":"
        public string Value { get; set; }
        public string Terminator { get; set; } // will always be ";"
        public override string ToString()
        {
            return $"{Opcode}{Seperator}{Value}{Terminator}";
        }

        #region EQUALITY_OVERLOADS
        public override bool Equals(object obj)
        {
            return Equals(obj as MeCardRawField);
        }

        public bool Equals(MeCardRawField other)
        {
            return !(other is null) &&
                   Opcode == other.Opcode &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            int hashCode = -714780983;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Opcode);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            return hashCode;
        }

        public static bool operator ==(MeCardRawField left, MeCardRawField right)
        {
            return EqualityComparer<MeCardRawField>.Default.Equals(left, right);
        }

        public static bool operator !=(MeCardRawField left, MeCardRawField right)
        {
            return !(left == right);
        }
        #endregion
    }
}
