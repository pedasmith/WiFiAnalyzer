using System;
using System.Collections.Generic;
using System.Text;

namespace MeCardParser
{
    public class MeCardTest
    {

        public static int TestMeCard()
        {
            int nerror = 0;

            nerror += Test_RawMeCard_One("WIFI:s:myssid;;", new MeCardRaw("WIFI", "s", "myssid"));

            nerror += StringUtility.TestNEndChars();
            return nerror;
        }

        private static int Test_RawMeCard_One(string url, MeCardRaw expected)
        {
            int nerror = 0;
            var actual = MeCardParser.Parse(url);
            if (actual != expected)
            {
                nerror++;
                Log($"ERROR: MECARD: Url={url} Expected={expected} Actual={actual}");
            }
            return nerror;
        }

        public static void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }


    }
}
