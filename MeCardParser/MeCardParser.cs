using System;
using static MeCardParser.MeCardRaw;

namespace MeCardParser
{
    internal class MeCardParser
    {
        public static MeCardRaw Parse(string urlString)
        {
            var retval = new MeCardRaw();
            if (urlString == null)
            {
                retval.IsValid = Validity.InvalidNull;
                retval.ErrorMessage = "WifiUrl was null";
                return retval;
            }
            if (urlString.Length < "WIFI:S:A;;".Length) // Absolute minimal WIFI: url
            {
                retval.IsValid = Validity.InvalidLength;
                retval.ErrorMessage = "WiFi url is too short";
                return retval;
            }
            var firstColon = urlString.IndexOf(':');
            if (firstColon < 0)
            {
                retval.IsValid = Validity.InvalidNoScheme;
                retval.ErrorMessage = "WiFi URL doesn't start with a scheme like wifi:";
                return retval;
            }
            var scheme = urlString.Substring(0, firstColon + 1).ToUpperInvariant();

            retval.Scheme = scheme.Substring(0, firstColon).ToUpperInvariant(); // should not include the ':'
            retval.SchemeSeperator = ":"; // known because it's what we looked for

            var len = urlString.Length;
            if (urlString[len - 1] != ';' || urlString[len - 2] != ';' || urlString[len - 3] == ';') // wrong number of semi-colons
            {
                retval.IsValid = Validity.InvalidEndSemicolons;
                retval.ErrorMessage = "WiFi URL doesn't end with exactly 2 semicolons";
                return retval;
            }
            retval.Terminator = ";"; // known because we literally just checked for that.

            var split = urlString.Substring(firstColon + 1).Split(';');
            foreach (var item in split)
            {
                // is, e.g., S:starpainter
                // Maybe it's the ACTION option. Or might be an empty string from the last two semicolons.
                if (item.Length == 0)
                {

                }
                else
                {
                    var nv = item.Split(new char[] { ':' });
                    if (nv.Length != 2)
                    {
                        retval.IsValid = Validity.InvalidColon;
                        retval.ErrorMessage = $"Item type should have 1 colon, not {nv.Length}";
                        return retval;
                    }
                    string value = nv[1];
                    string opcode = nv[0];
                    var field = new MeCardRawField(opcode, value);
                    field.Seperator = ":";
                    field.Terminator = ";";
                    retval.AddField(field);
                }
            }


            retval.IsValid = Validity.Valid;
            return retval;
        }
    }
}
