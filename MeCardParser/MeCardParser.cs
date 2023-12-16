using System;
using System.Runtime.CompilerServices;
using Windows.Foundation.Diagnostics;
using static MeCardParser.MeCardRaw;
namespace MeCardParser
{
    static class StringUtility
    {
        /// <summary>
        /// Given a string, return the numbe of 'lookFor' chars at the end
        /// </summary>
        /// <param name="value"></param>
        /// <param name="lookFor"></param>
        /// <returns></returns>
        public static int NEndChars(this string value, char lookFor = ';')
        {
            int retval = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (value[i] != lookFor) break;
                retval++;
            }
            return retval;
        }
        private static int TestNEndChars_One(string value, char lookFor, int expected)
        {
            int nerror = 0;
            var actual = value.NEndChars(lookFor);
            if (actual != expected)
            {
                nerror++;
                Log($"ERROR: NEndChars({value}) expected={expected} actual={actual}");
            }
            return nerror;
        }
        public static int TestNEndChars()
        {
            int nerror = 0;
            nerror += TestNEndChars_One("test", ';', 0);
            nerror += TestNEndChars_One("test;", ';', 1);
            nerror += TestNEndChars_One("test;;", ';', 2);

            // And some boundary conditions
            nerror += TestNEndChars_One("", ';', 0);
            nerror += TestNEndChars_One(";", ';', 1);
            nerror += TestNEndChars_One(";;", ';', 2);

            // A few 'whoopsie' tests :-)
            nerror += TestNEndChars_One(";;should be one;", ';', 1);
            nerror += TestNEndChars_One(";;should be one.", '.', 1);
            nerror += TestNEndChars_One(";;should be zero;", '.', 0);

            return nerror;
        }

        private static void Log(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
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

            // Patch up the number of semicolons. The actual spec says we need to end with exactly two.
            // But there are QR code creators that actually make incorrect values, which blows my mind.
            var nendsemicolon = urlString.NEndChars(';');
            if (nendsemicolon != 2)
            {
                switch (nendsemicolon)
                {
                    // Add one or two semicolons as needed.
                    case 0: urlString += ";;"; nendsemicolon = urlString.NEndChars(';');  break;
                    case 1: urlString += ";"; nendsemicolon = urlString.NEndChars(';');  break;
                }
            }

            var len = urlString.Length;
            if (nendsemicolon != 2) // wrong number of semi-colons
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
