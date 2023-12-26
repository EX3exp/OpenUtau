using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Api;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Core.Enunu {
    [Phonemizer("Enunu Korean Phonemizer", "ENUNU KO", "EX3", language:"KO")]
    public class EnunuKoreanPhonemizer : EnunuPhonemizer {
        readonly string PhonemizerType = "ENUNU KO";

    }
}
