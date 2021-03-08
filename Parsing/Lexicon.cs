using System.Collections.Generic;

namespace WASalesTax.Parsing
{
    /// <summary>
    /// A word list
    /// </summary>
    public class Lexicon
    {
        public Dictionary<string, string> dictionary = new Dictionary<string, string>();

        public static Lexicon GetLexicon(string[] txt)
        {
            var dict = new Dictionary<string, string>();
            for (int x = 0; x < txt.Length; x += 2)
            {
                dict.Add(txt[x], txt[x + 1]);
            }

            return new Lexicon { dictionary = dict };
        }

        public string Substitute(string str)
        {
            return dictionary.ContainsKey(str) ? dictionary[str] : null;
        }

        public bool Contains(string str)
        {
            return dictionary.ContainsKey(str);
        }

        public static string[] LexiconNormalDirectional = new string[] {
            "N", "N",
            "W", "W",
            "S", "S",
            "E", "E",
            "NW", "NW",
            "NE", "NE",
            "SW", "SW",
            "SE", "SE"
        };

        public static string[] LexiconDirectional = new string[] {
            "N", "N",
            "W", "W",
            "S", "S",
            "E", "E",
            "NW", "NW",
            "NE", "NE",
            "SW", "SW",
            "SE", "SE",
            "NORTH", "N",
            "WEST", "W",
            "SOUTH", "S",
            "EAST", "E",
            "NORTHWEST", "NW",
            "NORTHEAST", "NE",
            "SOUTHWEST", "SW",
            "SOUTHEAST", "SE",
            "SO", "S"
        };

        public static string[] LexiconCommonRoads = new string[] {
            "AV","AVE",
            "AVE","AVE",
            "AVENE","AVE",
            "AVEN","AVE",
            "AVENU","AVE",
            "AVENUE","AVE",
            "AVN","AVE",
            "AVNUE","AVE",
            "AVCT","AVCT",
            "BLVD","BLVD",
            "BOUL","BLVD",
            "BOULEVARD","BLVD",
            "BOULV","BLVD",
            "CIR","CIR",
            "CIRC","CIR",
            "CIRCL","CIR",
            "CIRCLE","CIR",
            "CRCL","CIR",
            "CRCLE","CIR",
            "CIRCLES","CIRS",
            "COURT","CT",
            "CREST","CRST",
            "CRST","CRST",
            "CRES","CRES",
            "CRT","CT",
            "CT","CT",
            "CK","CRK",
            "CR","CRK",
            "CREEK","CRK",
            "CRK","CRK",
            "DR","DR",
            "DRIV","DR",
            "DRIVE","DR",
            "DRV","DR",
            "EXT","EXT",
            "FRK","FRK",
            "FORK","FRK",
            "HIGHWAY","HWY",
            "HIGHWY","HWY",
            "HIWAY","HWY",
            "HIWY","HWY",
            "HWAY","HWY",
            "HWY","HWY",
            "KP","KP",
            "LA","LN",
            "LANE","LN",
            "LANES","LN",
            "LN","LN",
            "LP","LOOP",
            "LOOP","LOOP",
            "PARK","PARK",
            "PK","PARK",
            "PRK","PARK",
            "PARKS","PARK",
            "PL","PL",
            "PLACE","PL",
            "PLAIN","PLN",
            "PLN","PLN",
            "POINT","PT",
            "PR","PR",
            "PRAIRIE","PR",
            "PRARIE","PR",
            "PRR","PR",
            "PT","PT",
            "RD","RD",
            "ROAD","RD",
            "RDS","RDS",
            "RR","RR",
            "SQ","SQ",
            "SQR","SQ",
            "SQRE","SQ",
            "SQU","SQ",
            "SQUARE","SQ",
            "ST","ST",
            "STR","ST",
            "STREET","ST",
            "STRT","ST",
            "STHY","STHY",
            "STATEHIGHWAY","STHY",
            "STCT","STCT",
            "TER","TER",
            "TERR","TER",
            "TERRACE","TER",
            "TR","TRL",
            "TRAIL","TRL",
            "USHY","USHY",
            "USHIGHWAY","USHY",
            "VIS","VIS",
            "VIST","VIS",
            "VISTA","VIS",
            "VST","VIS",
            "VSTA","VIS",
            "WAY","WAY",
            "WY","WAY"
        };


        public static string[] LexiconUspsAbbr = new string[] {
            "ALLEE","ALY",
            "ALLEY","ALY",
            "ALLY","ALY",
            "ALY","ALY",
            "ANEX","ANX",
            "ANNEX","ANX",
            "ANNX","ANX",
            "ANX","ANX",
            "ARC","ARC",
            "ARCADE","ARC",
            "AV","AVE",
            "AVE","AVE",
            "AVEN","AVE",
            "AVENU","AVE",
            "AVENUE","AVE",
            "AVN","AVE",
            "AVNUE","AVE",
            "BAYOO","BYU",
            "BAYOU","BYU",
            "BCH","BCH",
            "BEACH","BCH",
            "BEND","BND",
            "BND","BND",
            "BLF","BLF",
            "BLUF","BLF",
            "BLUFF","BLF",
            "BLUFFS","BLFS",
            "BOT","BTM",
            "BOTTM","BTM",
            "BOTTOM","BTM",
            "BTM","BTM",
            "BLVD","BLVD",
            "BOUL","BLVD",
            "BOULEVARD","BLVD",
            "BOULV","BLVD",
            "BR","BR",
            "BRANCH","BR",
            "BRNCH","BR",
            "BRDGE","BRG",
            "BRG","BRG",
            "BRIDGE","BRG",
            "BRK","BRK",
            "BROOK","BRK",
            "BROOKS","BRKS",
            "BURG","BG",
            "BURGS","BGS",
            "BYP","BYP",
            "BYPA","BYP",
            "BYPAS","BYP",
            "BYPASS","BYP",
            "BYPS","BYP",
            "CAMP","CP",
            "CMP","CP",
            "CP","CP",
            "CANYN","CYN",
            "CANYON","CYN",
            "CNYN","CYN",
            "CYN","CYN",
            "CAPE","CPE",
            "CPE","CPE",
            "CAUSEWAY","CSWY",
            "CAUSWAY","CSWY",
            "CSWY","CSWY",
            "CEN","CTR",
            "CENT","CTR",
            "CENTER","CTR",
            "CENTR","CTR",
            "CENTRE","CTR",
            "CNTER","CTR",
            "CNTR","CTR",
            "CTR","CTR",
            "CENTERS","CTRS",
            "CIR","CIR",
            "CIRC","CIR",
            "CIRCL","CIR",
            "CIRCLE","CIR",
            "CRCL","CIR",
            "CRCLE","CIR",
            "CIRCLES","CIRS",
            "CLF","CLF",
            "CLIFF","CLF",
            "CLFS","CLFS",
            "CLIFFS","CLFS",
            "CLB","CLB",
            "CLUB","CLB",
            "COMMON","CMN",
            "COR","COR",
            "CORNER","COR",
            "CORNERS","CORS",
            "CORS","CORS",
            "COURSE","CRSE",
            "CRSE","CRSE",
            "COURT","CT",
            "CRT","CT",
            "CT","CT",
            "COURTS","CTS",
            "CTS","CTS",
            "COVE","CV",
            "CV","CV",
            "COVES","CVS",
            "CK","CRK",
            "CR","CRK",
            "CREEK","CRK",
            "CRK","CRK",
            "CRECENT","CRES",
            "CRES","CRES",
            "CRESCENT","CRES",
            "CRESENT","CRES",
            "CRSCNT","CRES",
            "CRSENT","CRES",
            "CRSNT","CRES",
            "CREST","CRST",
            "CROSSING","XING",
            "CRSSING","XING",
            "CRSSNG","XING",
            "XING","XING",
            "CROSSROAD","XRD",
            "CURVE","CURV",
            "DALE","DL",
            "DL","DL",
            "DAM","DM",
            "DM","DM",
            "DIV","DV",
            "DIVIDE","DV",
            "DV","DV",
            "DVD","DV",
            "DR","DR",
            "DRIV","DR",
            "DRIVE","DR",
            "DRV","DR",
            "DRIVES","DRS",
            "EST","EST",
            "ESTATE","EST",
            "ESTATES","ESTS",
            "ESTS","ESTS",
            "EXP","EXPY",
            "EXPR","EXPY",
            "EXPRESS","EXPY",
            "EXPRESSWAY","EXPY",
            "EXPW","EXPY",
            "EXPY","EXPY",
            "EXT","EXT",
            "EXTENSION","EXT",
            "EXTN","EXT",
            "EXTNSN","EXT",
            "EXTENSIONS","EXTS",
            "EXTS","EXTS",
            "FALL","FALL",
            "FALLS","FLS",
            "FLS","FLS",
            "FERRY","FRY",
            "FRRY","FRY",
            "FRY","FRY",
            "FIELD","FLD",
            "FLD","FLD",
            "FIELDS","FLDS",
            "FLDS","FLDS",
            "FLAT","FLT",
            "FLT","FLT",
            "FLATS","FLTS",
            "FLTS","FLTS",
            "FORD","FRD",
            "FRD","FRD",
            "FORDS","FRDS",
            "FOREST","FRST",
            "FORESTS","FRST",
            "FRST","FRST",
            "FORG","FRG",
            "FORGE","FRG",
            "FRG","FRG",
            "FORGES","FRGS",
            "FORK","FRK",
            "FRK","FRK",
            "FORKS","FRKS",
            "FRKS","FRKS",
            "FORT","FT",
            "FRT","FT",
            "FT","FT",
            "FREEWAY","FWY",
            "FREEWY","FWY",
            "FRWAY","FWY",
            "FRWY","FWY",
            "FWY","FWY",
            "GARDEN","GDN",
            "GARDN","GDN",
            "GDN","GDN",
            "GRDEN","GDN",
            "GRDN","GDN",
            "GARDENS","GDNS",
            "GDNS","GDNS",
            "GRDNS","GDNS",
            "GATEWAY","GTWY",
            "GATEWY","GTWY",
            "GATWAY","GTWY",
            "GTWAY","GTWY",
            "GTWY","GTWY",
            "GLEN","GLN",
            "GLN","GLN",
            "GLENS","GLNS",
            "GREEN","GRN",
            "GRN","GRN",
            "GREENS","GRNS",
            "GROV","GRV",
            "GROVE","GRV",
            "GRV","GRV",
            "GROVES","GRVS",
            "HARB","HBR",
            "HARBOR","HBR",
            "HARBR","HBR",
            "HBR","HBR",
            "HRBOR","HBR",
            "HARBORS","HBRS",
            "HAVEN","HVN",
            "HAVN","HVN",
            "HVN","HVN",
            "HEIGHT","HTS",
            "HEIGHTS","HTS",
            "HGTS","HTS",
            "HT","HTS",
            "HTS","HTS",
            "HIGHWAY","HWY",
            "HIGHWY","HWY",
            "HIWAY","HWY",
            "HIWY","HWY",
            "HWAY","HWY",
            "HWY","HWY",
            "HILL","HL",
            "HL","HL",
            "HILLS","HLS",
            "HLS","HLS",
            "HLLW","HOLW",
            "HOLLOW","HOLW",
            "HOLLOWS","HOLW",
            "HOLW","HOLW",
            "HOLWS","HOLW",
            "INLET","INLT",
            "INLT","INLT",
            "IS","IS",
            "ISLAND","IS",
            "ISLND","IS",
            "ISLANDS","ISS",
            "ISLNDS","ISS",
            "ISS","ISS",
            "ISLE","ISLE",
            "ISLES","ISLE",
            "JCT","JCT",
            "JCTION","JCT",
            "JCTN","JCT",
            "JUNCTION","JCT",
            "JUNCTN","JCT",
            "JUNCTON","JCT",
            "JCTNS","JCTS",
            "JCTS","JCTS",
            "JUNCTIONS","JCTS",
            "KEY","KY",
            "KY","KY",
            "KEYS","KYS",
            "KYS","KYS",
            "KNL","KNL",
            "KNOL","KNL",
            "KNOLL","KNL",
            "KNLS","KNLS",
            "KNOLLS","KNLS",
            "LAKE","LK",
            "LK","LK",
            "LAKES","LKS",
            "LKS","LKS",
            "LAND","LAND",
            "LANDING","LNDG",
            "LNDG","LNDG",
            "LNDNG","LNDG",
            "LA","LN",
            "LANE","LN",
            "LANES","LN",
            "LN","LN",
            "LGT","LGT",
            "LIGHT","LGT",
            "LIGHTS","LGTS",
            "LF","LF",
            "LOAF","LF",
            "LCK","LCK",
            "LOCK","LCK",
            "LCKS","LCKS",
            "LOCKS","LCKS",
            "LDG","LDG",
            "LDGE","LDG",
            "LODG","LDG",
            "LODGE","LDG",
            "LP","LOOP",
            "LOOP","LOOP",
            "LOOPS","LOOP",
            "MALL","MALL",
            "MANOR","MNR",
            "MNR","MNR",
            "MANORS","MNRS",
            "MNRS","MNRS",
            "MDW","MDW",
            "MEADOW","MDW",
            "MDWS","MDWS",
            "MEADOWS","MDWS",
            "MEDOWS","MDWS",
            "MEWS","MEWS",
            "MILL","ML",
            "ML","ML",
            "MILLS","MLS",
            "MLS","MLS",
            "MISSION","MSN",
            "MISSN","MSN",
            "MSN","MSN",
            "MSSN","MSN",
            "MOTORWAY","MTWY",
            "MNT","MT",
            "MOUNT","MT",
            "MT","MT",
            "MNTAIN","MTN",
            "MNTN","MTN",
            "MOUNTAIN","MTN",
            "MOUNTIN","MTN",
            "MTIN","MTN",
            "MTN","MTN",
            "MNTNS","MTNS",
            "MOUNTAINS","MTNS",
            "NCK","NCK",
            "NECK","NCK",
            "ORCH","ORCH",
            "ORCHARD","ORCH",
            "ORCHRD","ORCH",
            "OVAL","OVAL",
            "OVL","OVAL",
            "OVERPASS","OPAS",
            "PARK","PARK",
            "PK","PARK",
            "PRK","PARK",
            "PARKS","PARK",
            "PARKWAY","PKWY",
            "PARKWY","PKWY",
            "PKWAY","PKWY",
            "PKWY","PKWY",
            "PKY","PKWY",
            "PARKWAYS","PKWY",
            "PKWYS","PKWY",
            "PASS","PASS",
            "PASSAGE","PSGE",
            "PATH","PATH",
            "PATHS","PATH",
            "PIKE","PIKE",
            "PIKES","PIKE",
            "PINE","PNE",
            "PINES","PNES",
            "PNES","PNES",
            "PL","PL",
            "PLACE","PL",
            "PLAIN","PLN",
            "PLN","PLN",
            "PLAINES","PLNS",
            "PLAINS","PLNS",
            "PLNS","PLNS",
            "PLAZA","PLZ",
            "PLZ","PLZ",
            "PLZA","PLZ",
            "POINT","PT",
            "PT","PT",
            "POINTS","PTS",
            "PTS","PTS",
            "PORT","PRT",
            "PRT","PRT",
            "PORTS","PRTS",
            "PRTS","PRTS",
            "PR","PR",
            "PRAIRIE","PR",
            "PRARIE","PR",
            "PRR","PR",
            "RAD","RADL",
            "RADIAL","RADL",
            "RADIEL","RADL",
            "RADL","RADL",
            "RAMP","RAMP",
            "RANCH","RNCH",
            "RANCHES","RNCH",
            "RNCH","RNCH",
            "RNCHS","RNCH",
            "RAPID","RPD",
            "RPD","RPD",
            "RAPIDS","RPDS",
            "RPDS","RPDS",
            "REST","RST",
            "RST","RST",
            "RDG","RDG",
            "RDGE","RDG",
            "RIDGE","RDG",
            "RDGS","RDGS",
            "RIDGES","RDGS",
            "RIV","RIV",
            "RIVER","RIV",
            "RIVR","RIV",
            "RVR","RIV",
            "RD","RD",
            "ROAD","RD",
            "RDS","RDS",
            "ROADS","RDS",
            "ROUTE","RTE",
            "ROW","ROW",
            "RUE","RUE",
            "RUN","RUN",
            "SHL","SHL",
            "SHOAL","SHL",
            "SHLS","SHLS",
            "SHOALS","SHLS",
            "SHOAR","SHR",
            "SHORE","SHR",
            "SHR","SHR",
            "SHOARS","SHRS",
            "SHORES","SHRS",
            "SHRS","SHRS",
            "SKYWAY","SKWY",
            "SPG","SPG",
            "SPNG","SPG",
            "SPRING","SPG",
            "SPRNG","SPG",
            "SPGS","SPGS",
            "SPNGS","SPGS",
            "SPRINGS","SPGS",
            "SPRNGS","SPGS",
            "SPUR","SPUR",
            "SPURS","SPUR",
            "SQ","SQ",
            "SQR","SQ",
            "SQRE","SQ",
            "SQU","SQ",
            "SQUARE","SQ",
            "SQRS","SQS",
            "SQUARES","SQS",
            "STA","STA",
            "STATION","STA",
            "STATN","STA",
            "STN","STA",
            "STRA","STRA",
            "STRAV","STRA",
            "STRAVE","STRA",
            "STRAVEN","STRA",
            "STRAVENUE","STRA",
            "STRAVN","STRA",
            "STRVN","STRA",
            "STRVNUE","STRA",
            "STREAM","STRM",
            "STREME","STRM",
            "STRM","STRM",
            "ST","ST",
            "STR","ST",
            "STREET","ST",
            "STRT","ST",
            "STREETS","STS",
            "SMT","SMT",
            "SUMIT","SMT",
            "SUMITT","SMT",
            "SUMMIT","SMT",
            "TER","TER",
            "TERR","TER",
            "TERRACE","TER",
            "THROUGHWAY","TRWY",
            "TRACE","TRCE",
            "TRACES","TRCE",
            "TRCE","TRCE",
            "TRACK","TRAK",
            "TRACKS","TRAK",
            "TRAK","TRAK",
            "TRK","TRAK",
            "TRKS","TRAK",
            "TRAFFICWAY","TRFY",
            "TRFY","TRFY",
            "TR","TRL",
            "TRAIL","TRL",
            "TRAILS","TRL",
            "TRL","TRL",
            "TRLS","TRL",
            "TUNEL","TUNL",
            "TUNL","TUNL",
            "TUNLS","TUNL",
            "TUNNEL","TUNL",
            "TUNNELS","TUNL",
            "TUNNL","TUNL",
            "TPK","TPKE",
            "TPKE","TPKE",
            "TRNPK","TPKE",
            "TRPK","TPKE",
            "TURNPIKE","TPKE",
            "TURNPK","TPKE",
            "UNDERPASS","UPAS",
            "UN","UN",
            "UNION","UN",
            "UNIONS","UNS",
            "VALLEY","VLY",
            "VALLY","VLY",
            "VLLY","VLY",
            "VLY","VLY",
            "VALLEYS","VLYS",
            "VLYS","VLYS",
            "VDCT","VIA",
            "VIA","VIA",
            "VIADCT","VIA",
            "VIADUCT","VIA",
            "VIEW","VW",
            "VW","VW",
            "VIEWS","VWS",
            "VWS","VWS",
            "VILL","VLG",
            "VILLAG","VLG",
            "VILLAGE","VLG",
            "VILLG","VLG",
            "VILLIAGE","VLG",
            "VLG","VLG",
            "VILLAGES","VLGS",
            "VLGS","VLGS",
            "VILLE","VL",
            "VL","VL",
            "VIS","VIS",
            "VIST","VIS",
            "VISTA","VIS",
            "VST","VIS",
            "VSTA","VIS",
            "WALK","WALK",
            "WALKS","WALK",
            "WALL","WALL",
            "WAY","WAY",
            "WY","WAY",
            "WAYS","WAYS",
            "WELL","WL",
            "WELLS","WLS",
            "WLS","WLS"
        };

        public static string[] LexiconSecondaryUnit = new string[] {
            "APARTMENT","APT",
            "APT","APT",
            "BASEMENT","BSMT",
            "BSMT","BSMT",
            "BUILDING","BLDG",
            "BLDG","BLDG",
            "DEPARTMENT","DEPT",
            "DEPT","DEPT",
            "FLOOR","FL",
            "FL","FL",
            "FRONT","FRNT",
            "FRNT","FRNT",
            "HANGAR","HNGR",
            "HNGR","HNGR",
            "LOBBY","LBBY",
            "LBBY","LBBY",
            "LOT","LOT",
            "LOWER","LOWR",
            "LOWR","LOWR",
            "OFFICE","OFC",
            "OFC","OFC",
            "PENTHOUSE","PH",
            "PH","PH",
            "PIER","PIER",
            "REAR","REAR",
            "ROOM","RM",
            "RM","RM",
            "SIDE","SIDE",
            "SLIP","SLIP",
            "SPACE","SPC",
            "SPC","SPC",
            "STOP","STOP",
            "SUITE","STE",
            "STE","STE",
            "TRAILER","TRLR",
            "TRLR","TRLR",
            "UNIT","UNIT",
            "UPPER","UPPR",
            "UPPR","UPPR"
        };

        public static string[] LexiconOrdinalWord = new string[] {
            "FIRST", "1ST",
            "SECOND", "2ND",
            "THIRD", "3RD",
            "FOURTH", "4TH",
            "FIFTH", "5TH",
            "SIXTH", "6TH",
            "SEVENTH", "7TH",
            "EIGHTH", "8TH",
            "NINETH", "9TH",
            "TENTH", "10TH"
        };
    }
}
