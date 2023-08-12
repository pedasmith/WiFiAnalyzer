using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Web;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Networking.NetworkOperators;
using static MeCardParser.MeCardRawWiFi;

namespace MeCardParser
{
    /// <summary>
    /// Helpers for WIFI: and WIFISETUP: URL schemes. The WIFI: url as of 2022-09-27 matches the WPA3 official scheme, even though it's bad and not widely followed.
    /// </summary>
    public class WiFiUrl
    {
        public WiFiUrl(NetworkOperatorTetheringAccessPointConfiguration value)
        {
            Ssid = value.Ssid;
            Password = value.Passphrase;
            IsValid = Validity.Valid;
        }
        public WiFiUrl(string ssid, string password)
        {
            Ssid = ssid;
            Password = password;
            WiFiAuthType = "WPA";
            IsValid = Validity.Valid;
        }
        /// <summary>
        /// Parse a WiFiUrl from a string like WIFI:S:starpainter;P:deeznuts;;
        /// Catches every possible error.
        /// </summary>
        /// <param name="urlString"></param>
        public WiFiUrl(string urlString)
        {
            var meCard = MeCardParser.Parse(urlString);
            MeCardRawWiFi.ValidateAsWiFi(meCard);
            if (meCard.IsValid != MeCardRaw.Validity.Valid)
            {
                IsValid = (MeCardRawWiFi.Validity)meCard.IsValidWiFi;
                ErrorMessage = meCard.ErrorMessage;
                return;
            }

            // URL was a totally valid WIFI: or WIFISETUP: url; update everything.
            MeCardRawWiFi.WiFiFillDefaults(meCard);
            Scheme = meCard.SchemeCanonical;
            var fieldvalue = meCard.GetFieldValue("ACTION", "CONNECT"); // QUESTION: what if user had %20%34%43=="true"? Should we decode this?
            Action = fieldvalue; ;

            fieldvalue = meCard.GetFieldValue("H", ""); // QUESTION: what if user had %20%34%43=="true"? Should we decode this?
            Hidden = fieldvalue == "true";

            fieldvalue = meCard.GetFieldValue("I", null);
            if (fieldvalue != null) Id = HttpUtility.UrlDecode(fieldvalue);

            PublicKey = meCard.GetFieldValue("K", null); // This is BASE64 string and should be decoded from that.

            fieldvalue = meCard.GetFieldValue("P", null);
            if (fieldvalue != null) Password = HttpUtility.UrlDecode(fieldvalue);

            fieldvalue = meCard.GetFieldValue("R", null);
            if (fieldvalue != null) TRDisable = fieldvalue; // This is a HEX string and should be decoded from that.

            fieldvalue = meCard.GetFieldValue("S", null);
            if (fieldvalue != null) Ssid = HttpUtility.UrlDecode(fieldvalue);

            fieldvalue = meCard.GetFieldValue("T", null);
            if (fieldvalue != null) WiFiAuthType = fieldvalue; // QUESTION: not decoded??

            IsValid = Validity.Valid;
        }



        public WiFiUrl(string scheme, string wifiType, string trdisable, string ssid, bool hidden, string id, string password, string publicKey)
        {
            Scheme = scheme;
            WiFiAuthType = wifiType;
            TRDisable = trdisable;
            Ssid = ssid;
            Hidden = hidden;
            Id = id;
            Password = password;
            PublicKey = publicKey;
        }
        public NetworkOperatorTetheringAccessPointConfiguration AsAccessPointConfiguration()
        {
            if (this.IsValid != Validity.Valid) return null;
            var retval = new NetworkOperatorTetheringAccessPointConfiguration()
            {
                Passphrase = this.Password,
                Ssid = this.Ssid,
                Band = TetheringWiFiBand.Auto, // Idiotic: no band specified
            };
            return retval;
        }
        public override string ToString()
        {
            var retval = $"{Scheme}:";
            if (Scheme!="WIFI" || Action!="CONNECT")
            {
                if (Action != null) retval += "ACTION:" + Action + ";"; // actions never have a bad character.
            }
            if (WiFiAuthType!=null) retval += "T:" + WiFiAuthType + ";";
            if (TRDisable!=null) retval += "R:" + TRDisable + ";";
            if (Ssid!=null) retval += "S:" + HttpUtility.UrlEncode(Ssid) + ";";
            if (Hidden) retval += "H:true;";
            if (Id!=null) retval += "I:" + HttpUtility.UrlEncode(Id) + ";";
            if (Password!=null) retval += "P:" + HttpUtility.UrlEncode(Password) + ";";
            if (PublicKey!=null) retval += "K:" + PublicKey + ";";
            retval += ";"; // Idiotic. URL always ends with an extra ';'
            return retval;
        }

        private static int TestOneFailure(string url, Validity expected, string ssid = null, string password = null, TestFlags flags = TestFlags.All)
        {
            int nerror = 0;
            var actual = new WiFiUrl(url);
            if (actual.IsValid != expected)
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl ({url}) expected {expected} actual {actual.IsValid}");
            }
            else if (actual.IsValid == Validity.Valid) // if it's valid, check the ssid and password
            {
                if (actual.Ssid != ssid)
                {
                    nerror += 1;
                    System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl ({url}) SSID expected {ssid} actual {actual.Ssid}");
                }
                if (actual.Password != password)
                {
                    nerror += 1;
                    System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl ({url}) Password expected {password} actual {actual.Password}");
                }
                if (!flags.HasFlag(TestFlags.NoRoundtrip))
                {
                    var roundTrip = actual.ToString(); // should be identical to the original
                    if (roundTrip != url)
                    {
                        nerror += 1;
                        System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl ({url}) roundtrip={roundTrip}");
                    }
                }
            }
            return nerror;
        }

        private static int TestRoundtrip(string ssid, string password)
        {
            int nerror = 0;
            var url = new WiFiUrl(ssid, password);
            var urlstr = url.ToString();
            var roundtripUrl = new WiFiUrl(urlstr);
            if (roundtripUrl.IsValid != Validity.Valid)
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl Roundtrip({ssid}, {password}) made in invalid url from {url} error {roundtripUrl.IsValid}");
            }
            if (ssid != roundtripUrl.Ssid)
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl Roundtrip({ssid}, {password})-->{urlstr}-->{roundtripUrl}-->{roundtripUrl.Ssid}");
            }
            if (password != roundtripUrl.Password)
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: WiFiUrl Roundtrip({ssid}, {password})-->{urlstr}-->{roundtripUrl}-->{roundtripUrl.Password}");
            }

            return nerror;
        }
        enum TestFlags { All=0, NoRoundtrip=1 };
        public static int Test()
        {
            int nerror = 0;
            // Valid parsing
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts");
            nerror += TestOneFailure("WIFI:S:starpainter;;", Validity.Valid, "starpainter", null);
            nerror += TestOneFailure("wifi:S:starpainter;;", Validity.Valid, "starpainter", null, TestFlags.NoRoundtrip); // WIFI: is a valid scheme.

            // real-world parsing
            nerror += TestOneFailure("WIFI:S:ORBI36;T:WPA2;P:mypassword;SN:2WEMC;SK:RBK22-100NAS;MAC:21FF5394D278;;", Validity.Valid, "ORBI36", "mypassword", TestFlags.NoRoundtrip);


            // Test all of the opcodes. S and P are already tested.
            nerror += TestOneFailure("WIFI:T:WPA;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one T
            nerror += TestOneFailure("WIFI:R:Af01;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one R
            nerror += TestOneFailure("WIFI:S:starpainter;H:true;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one H
            nerror += TestOneFailure("WIFI:S:starpainter;I:value;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one I
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;K:isbase64;;", Validity.Valid, "starpainter", "deeznuts"); // verify one K

            // Catch the big errors
            nerror += TestOneFailure(null, Validity.InvalidNull);
            nerror += TestOneFailure("http://example.com/", Validity.InvalidWrongScheme);
            nerror += TestOneFailure("WIFI:", Validity.InvalidLength);

            // Catch parsing errors
            nerror += TestOneFailure("WIFI:S:has\rcr;;", Validity.InvalidNotUrlEncoded);
            nerror += TestOneFailure("WIFI:S:starpainter;P:has\rcr;;", Validity.InvalidNotUrlEncoded);
            nerror += TestOneFailure("WIFI:T:*;S:starpainter;P:deeznuts;;", Validity.InvalidNotUnreserved); 
            nerror += TestOneFailure("WIFI:R:ZZ;S:starpainter;P:has\rcr;;", Validity.InvalidNotHex);

            // 2023-08-12 loosen H field to allow anything. Androi sets H:false
            // nerror += TestOneFailure("WIFI:S:starpainter;H:t;P:deeznuts;;", Validity.InvalidNotTrue);
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;K:(not64);;", Validity.InvalidNotBase64); 

            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;", Validity.InvalidEndSemicolons); // needs two, not one
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts", Validity.InvalidEndSemicolons); // needs two, not none

            // Catch higher level issues
            //2023-08-12 don't test for order any more; android is making out-of-order URLs
            //nerror += TestOneFailure("WIFI:P:deeznuts;S:starpainter;;", Validity.InvalidOpcodeOrder);
            nerror += TestOneFailure("WIFI:S:starpainter;S:starpainter;P:deeznuts;;", Validity.InvalidOpcodeDuplicate);
            nerror += TestOneFailure("WIFI:P:deeznuts;;", Validity.InvalidNoSsid);

            // Terrible error catching that is mandated by spec
            nerror += TestOneFailure("WIFI:S:starpainter;P:has%ZZbadpercent;;", Validity.Valid, "starpainter", "has%ZZbadpercent", TestFlags.NoRoundtrip); // Idiotic: ? percent decoding doesn't catch failures
            nerror += TestOneFailure("WIFI://T:WPA;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts", TestFlags.NoRoundtrip); // Idiotic; can't catch this error easily

            // Test round-trips
            nerror += TestRoundtrip("starpainter", "deeznuts");
            nerror += TestRoundtrip("", "deeznuts");
            nerror += TestRoundtrip("starpainter", "");
            nerror += TestRoundtrip("star\x0000painter", "deez\x0000nuts");
            nerror += TestRoundtrip("star;;painter", "deez;;nuts");

            // Test WIFISETUP:
            nerror += TestOneFailure("WIFISETUP:ACTION:SETUP;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts");
            nerror += TestOneFailure("WIFISETUP:ACTION:CONNECT;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts");


            //TODO: are the opcodes case sensitive?

            return nerror;
        }
        //public enum Validity {  Valid, InvalidOther, InvalidNull, InvalidLength, InvalidWrongScheme,  // these are all "can't even try to parse"
        //    InvalidNotUrlEncoded, InvalidNotUnreserved, InvalidNotHex, InvalidNotTrue, InvalidNotBase64, InvalidEndSemicolons, InvalidColon, // these are "low level details are wrong"
        //    InvalidNoSsid, InvalidOpcodeDuplicate, InvalidOpcodeOrder // these are all high-level opcode issues
        //};
        public Validity IsValid { get; set; } = Validity.InvalidOther;
        public string ErrorMessage { get; set; } = null;

        public bool ActionIsConnect { get { return Action == "CONNECT"; } }
        public bool ActionIsSetup { get { return Action == "SETUP"; } }
        public string Scheme { get; set; } = "WIFI"; // is always WIFI or WIFISETUP. Idiotic: Schemes should be lowercase per https://www.rfc-editor.org/rfc/rfc7595


        /// <summary>
        /// New field, ACTION=CONNECT; or ACTION=SETUP;. Default is CONNECT for WIFI: scheme (and SETUP for WIFISETUP: scheme)
        /// </summary>
        public string Action { get; set; } = "CONNECT";

        /// <summary>
        /// WiFi Security type. When present, must be WPA which includes WPA3. When not present, implies open (or similar), which won't work on Android
        /// </summary>
        public string WiFiAuthType { get; set; } // type = “T:” *(unreserved) ; security type. Must be WPA
        /// <summary>
        /// Property saved is raw HEX digits
        /// </summary>
        public string TRDisable { get; set; } // trdisable = “R:” *(HEXDIG) ; Transition Disable value
        /// <summary>
        /// Property is the unencoded (raw) format
        /// </summary>
        public string Ssid { get; set; } // ssid = “S:” *(printable / pct-encoded) ; SSID of the network
        public bool Hidden { get; set; } //hidden = “H:true” ; when present, indicates a hidden (stealth) SSID is used 
        /// <summary>
        /// Property is the unencoded (raw) format
        /// </summary>
        public string Id { get; set; } // id = “I:” *(printable / pct-encoded) ; UTF-8 encoded password identifier, present if the password has an SAE password identifier
        /// <summary>
        /// Property is the unencoded (raw) format
        /// </summary>
        public string Password { get; set; } // password = “P:” *(printable / pct-encoded) ; password, present for password-based authentication
        /// <summary>
        /// Property is the BASE64 encoded value; it's not checked to be sure it's base64
        /// </summary>
        public string PublicKey { get; set; } // public-key = “K:” *PKCHAR ; DER of ASN.1 SubjectPublicKeyInfo in compressed form and encoded in “base64” as per [6], present when the network supports SAE-PK, else absent
    }
}
