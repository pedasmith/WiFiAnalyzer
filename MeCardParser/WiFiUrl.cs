using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Web;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Networking.NetworkOperators;

namespace MeCardParsers
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
        public WiFiUrl(string urlString)
        {
            if (urlString == null)
            {
                IsValid = Validity.InvalidNull;
                ErrorMessage = "WifiUrl was null";
                return;
            }
            if (urlString.Length < "WIFI:S:A;;".Length) // Absolute minimal WIFI: url
            {
                IsValid = Validity.InvalidLength;
                ErrorMessage = "WiFi url is too short";
                return;
            }
            var firstColon= urlString.IndexOf(':');
            if (firstColon < 0)
            {
                IsValid = Validity.InvalidWrongScheme;
                ErrorMessage = "WiFi URL doesn't start with a scheme like wifi:";
                return;
            }
            var scheme = urlString.Substring(0, firstColon+1).ToUpperInvariant();
            if (scheme != "WIFI:" && scheme != "WIFISETUP:")
            {
                IsValid = Validity.InvalidWrongScheme;
                ErrorMessage = "WiFi URL doesn't start with WIFI:";
                return;
            }
            Scheme = scheme.Substring(0, firstColon).ToUpperInvariant(); // should not include the ':'
            switch (Scheme)
            {
                case "WIFI": Action = "CONNECT"; break;
                case "WIFISETUP": Action = "SETUP"; break;
            }
            var len = urlString.Length;
            if (urlString[len-1] != ';' || urlString[len-2] != ';' || urlString[len-3] == ';') // wrong number of semi-colons
            {
                IsValid = Validity.InvalidEndSemicolons;
                ErrorMessage = "WiFi URL doesn't end with exactly 2 semicolons";
                return;
            }

            Dictionary<char, int> reqiredOrder = new Dictionary<char, int>()
            {
                { 'T', 0 },
                { 'R', 1 },
                { 'S', 2 },
                { 'H', 3 },
                { 'I', 4 },
                { 'P', 5 },
                { 'K', 6 },
            };
            int lastFoundOrder = -1;
            var foundOpcodes = new HashSet<int>();

            var split = urlString.Substring(firstColon+1).Split(';');
            foreach (var item in split)
            {
                // is, e.g., S:starpainter
                if (item.Length >= 2 && item[1] == ':') // In the case of e.g. S:; or P:; the field should be set to zero-length string, not null.
                {
                    // One of the known values
                    string value = item.Substring(2);
                    char opcode = item[0];
                    int newOrder = -2;
                    var haveorder = reqiredOrder.TryGetValue(opcode, out newOrder);
                    if (haveorder) // if the opcode isn't known then it can be in any order
                    {
                        if (foundOpcodes.Contains(newOrder))
                        {
                            IsValid = Validity.InvalidOpcodeDuplicate;
                            ErrorMessage = "WiFi URL contains a duplicate opcode";
                            return;
                        }
                        if (newOrder <= lastFoundOrder)
                        {
                            IsValid = Validity.InvalidOpcodeOrder;
                            ErrorMessage = "WiFi URL is not in correct order (T, R, S, H I, P, K)";
                            return;
                        }
                        foundOpcodes.Add(newOrder);
                        lastFoundOrder = newOrder;
                    }

                    switch (opcode) 
                    {
                        case 'H':
                            if (value != "true")
                            {
                                IsValid = Validity.InvalidNotTrue;
                                ErrorMessage = $"Item type {opcode} can only be true";
                                return;
                            }
                            Hidden = true;
                            break;
                        case 'I':
                            if (!value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            value = HttpUtility.UrlDecode(value);
                            if (value == null)
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            Id = value;
                            break;
                        case 'K':
                            if (!value.IsPKChar())
                            {
                                IsValid = Validity.InvalidNotBase64;
                                ErrorMessage = $"Item type {opcode} has not bsase 64";
                                return;
                            }
                            PublicKey = value;
                            break;
                        case 'P': // Password
                            if (!value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            value = HttpUtility.UrlDecode(value);
                            if (value == null)
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            Password = value;
                            break;
                        case 'R':
                            if (!value.IsHexDigit())
                            {
                                IsValid = Validity.InvalidNotHex;
                                ErrorMessage = $"Item type {opcode} has incorrect non-hex chars";
                                return;
                            }
                            TRDisable = value;
                            break;
                        case 'S': // SSID
                            if (!value.IsPercentEncoded()) // UrlDecode doesn't do this check.
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            value = HttpUtility.UrlDecode(value);
                            if (value == null)
                            {
                                IsValid = Validity.InvalidNotUrlEncoded;
                                ErrorMessage = $"Item type {opcode} was not URL encoded";
                                return;
                            }
                            Ssid = value;
                            break;
                        case 'T': // Type
                            if (!value.IsUnreserved()) 
                            {
                                IsValid = Validity.InvalidNotUnreserved;
                                ErrorMessage = $"Item type {opcode} has non-unreserved characters";
                                return;
                            }
                            WiFiAuthType = value;
                            break;
                        default:
                            break; // Unknowns should just be accepted. TODO: save in an unknowns array
                    }
                }
                else // either a known item that's malformed or an unknown item or a multi-char op like ACTION
                {
                    // Maybe it's the ACTION option. Or might be an empty string from the last two semicolons.
                    if (item.Length == 0)
                    {

                    }
                    else
                    {
                        var nv = item.Split(new char[] { ':' });
                        if (nv.Length != 2)
                        {
                            IsValid = Validity.InvalidColon;
                            ErrorMessage = $"Item type should have 1 colon, not {nv.Length}";
                            return;
                        }
                        string value = nv[1];
                        string opcode = nv[0];
                        switch (opcode)
                        {
                            case "ACTION": Action = value; break;
                        }
                    }
                }
            }
            if (Ssid == null)
            {
                IsValid = Validity.InvalidNoSsid;
                ErrorMessage = "WiFi URL does not include a required S: value";
                return;
            }

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

            // Test all of the opcodes. S and P are already tested.
            nerror += TestOneFailure("WIFI:T:WPA;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one T
            nerror += TestOneFailure("WIFI:R:Af01;S:starpainter;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one R
            nerror += TestOneFailure("WIFI:S:starpainter;H:true;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one H
            nerror += TestOneFailure("WIFI:S:starpainter;I:value;P:deeznuts;;", Validity.Valid, "starpainter", "deeznuts"); // verify one I
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;K:isbase64;;", Validity.Valid, "starpainter", "deeznuts"); // verify one K

            // Catch the big errors
            nerror += TestOneFailure(null, Validity.InvalidNull);
            nerror += TestOneFailure("http://example.com", Validity.InvalidWrongScheme);
            nerror += TestOneFailure("WIFI:", Validity.InvalidLength);

            // Catch parsing errors
            nerror += TestOneFailure("WIFI:S:has\rcr;;", Validity.InvalidNotUrlEncoded);
            nerror += TestOneFailure("WIFI:S:starpainter;P:has\rcr;;", Validity.InvalidNotUrlEncoded);
            nerror += TestOneFailure("WIFI:T:*;S:starpainter;P:deeznuts;;", Validity.InvalidNotUnreserved); 
            nerror += TestOneFailure("WIFI:R:ZZ;S:starpainter;P:has\rcr;;", Validity.InvalidNotHex);
            nerror += TestOneFailure("WIFI:S:starpainter;H:t;P:deeznuts;;", Validity.InvalidNotTrue);
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;K:(not64);;", Validity.InvalidNotBase64); 

            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts;", Validity.InvalidEndSemicolons); // needs two, not one
            nerror += TestOneFailure("WIFI:S:starpainter;P:deeznuts", Validity.InvalidEndSemicolons); // needs two, not none

            // Catch higher level issues
            nerror += TestOneFailure("WIFI:P:deeznuts;S:starpainter;;", Validity.InvalidOpcodeOrder);
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

            //TODO: are the opcodes case sensitive?

            return nerror;
        }
        public enum Validity {  Valid, InvalidOther, InvalidNull, InvalidLength, InvalidWrongScheme,  // these are all "can't even try to parse"
            InvalidNotUrlEncoded, InvalidNotUnreserved, InvalidNotHex, InvalidNotTrue, InvalidNotBase64, InvalidEndSemicolons, InvalidColon, // these are "low level details are wrong"
            InvalidNoSsid, InvalidOpcodeDuplicate, InvalidOpcodeOrder // these are all high-level opcode issues
        };
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
