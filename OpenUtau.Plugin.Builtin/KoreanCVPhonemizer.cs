using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;


// TODO: refactoring code
namespace OpenUtau.Plugin.Builtin {
    /// Phonemizer for 'KOR CV' ///
    [Phonemizer("Korean CV Phonemizer", "KO CV", "EX3", language: "KO")]

    public class KoreanCVPhonemizer : Core.BaseKoreanPhonemizer {

        // 1. Load Singer and Settings
        private KoreanCVIniSetting koreanCVIniSetting; // Setting object

        private bool isUsingShi = false;
        private bool isUsing_aX = false;
        private bool isUsing_i = false;
        private bool isRentan = false;



        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }

            koreanCVIniSetting = new KoreanCVIniSetting();
            koreanCVIniSetting.initialize(singer, "ko-CV.ini");

            isUsingShi = koreanCVIniSetting.isUsingShi();
            isUsing_aX = koreanCVIniSetting.isUsing_aX();
            isUsing_i = koreanCVIniSetting.isUsing_i();
            isRentan = koreanCVIniSetting.isRentan();
        }




        private class KoreanCVIniSetting : IniSetting {
            protected override void iniSetUp(IniFile iniFile) {
                // ko-CV.ini
                setOrReadThisValue("CV", "Use rentan", false); // 연단음 사용 유무 - 기본값 false
                setOrReadThisValue("CV", "Use 'shi' for '시'(otherwise 'si')", false); // 시를 [shi]로 표기할 지 유무 - 기본값 false
                setOrReadThisValue("CV", "Use 'i' for '의'(otherwise 'eui')", false); // 의를 [i]로 표기할 지 유무 - 기본값 false
                setOrReadThisValue("BATCHIM", "Use 'aX' instead of 'a X'", false); // 받침 표기를 a n 처럼 할 지 an 처럼 할지 유무 - 기본값 false(=a n 사용)
            }

            public bool isRentan() {
                bool isRentan = iniFile["CV"]["Use rentan"].ToBool();
                return isRentan;
            }

            public bool isUsingShi() {
                bool isUsingShi = iniFile["CV"]["Use 'shi' for '시'(otherwise 'si')"].ToBool();
                return isUsingShi;
            }

            public bool isUsing_aX() {
                bool isUsing_aX = iniFile["BATCHIM"]["Use 'aX' instead of 'a X'"].ToBool();
                return isUsing_aX;
            }

            public bool isUsing_i() {
                bool isUsing_i = iniFile["CV"]["Use 'i' for '의'(otherwise 'eui')"].ToBool();
                return isUsing_i;
            }

        }
        private class CV {
            static readonly string[] PHONEME_TYPES = new string[]{
                "none", "voiced", "aspirate", "fortis", "fricative", "liquid", "nasal"
                }; // 음소없음, 유성음, 파열음&파찰음, 경음, 마찰음, 유음, 비음 
            static readonly Dictionary<string, string[]> FIRST_CONSONANTS = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g", "voiced"}},
                {"ㄲ", new string[2]{"gg", "fortis"}},
                {"ㄴ", new string[2]{"n", "nasal"}},
                {"ㄷ", new string[2]{"d", "voiced"}},
                {"ㄸ", new string[2]{"dd", "fortis"}},
                {"ㄹ", new string[2]{"r", "liquid"}},
                {"ㅁ", new string[2]{"m", "nasal"}},
                {"ㅂ", new string[2]{"b", "voiced"}},
                {"ㅃ", new string[2]{"bb", "fortis"}},
                {"ㅅ", new string[2]{"s", "fricative"}},
                {"ㅆ", new string[2]{"ss", "fricative"}},
                {"ㅇ", new string[2]{"", "none"}},
                {"ㅈ", new string[2]{"j", "voiced"}},
                {"ㅉ", new string[2]{"jj", "fortis"}},
                {"ㅊ", new string[2]{"ch", "aspirate"}},
                {"ㅋ", new string[2]{"k", "aspirate"}},
                {"ㅌ", new string[2]{"t", "aspirate"}},
                {"ㅍ", new string[2]{"p", "aspirate"}},
                {"ㅎ", new string[2]{"h", "fricative"}},
                {" ", new string[2]{"", "null"}},
                {"null", new string[2]{"", ""}} // 뒤 글자가 없을 때를 대비
                };


            static readonly Dictionary<string, string[]> MIDDLE_VOWELS = new Dictionary<string, string[]>(){
                {"ㅏ", new string[3]{"a", "", "a"}},
                {"ㅐ", new string[3]{"e", "", "e"}},
                {"ㅑ", new string[3]{"ya", "y", "a"}},
                {"ㅒ", new string[3]{"ye", "y", "e"}},
                {"ㅓ", new string[3]{"eo", "", "eo"}},
                {"ㅔ", new string[3]{"e", "", "e"}},
                {"ㅕ", new string[3]{"yeo", "y", "eo"}},
                {"ㅖ", new string[3]{"ye", "y", "e"}},
                {"ㅗ", new string[3]{"o", "", "o"}},
                {"ㅘ", new string[3]{"wa", "w", "a"}},
                {"ㅙ", new string[3]{"we", "w", "e"}},
                {"ㅛ", new string[3]{"yo", "y", "o"}},
                {"ㅜ", new string[3]{"u", "", "u"}},
                {"ㅝ", new string[3]{"weo", "w", "eo"}},
                {"ㅞ", new string[3]{"we", "w", "e"}},
                {"ㅟ", new string[3]{"wi", "w", "i"}},
                {"ㅠ", new string[3]{"yu", "y", "u"}},
                {"ㅡ", new string[3]{"eu", "", "eu"}},
                {"ㅢ", new string[3]{"i", "", "i"}}, // ㅢ는 ㅣ로 발음
                {"ㅣ", new string[3]{"i", "", "i"}},
                {" ", new string[3]{"", "", ""}},
                {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
                };

            static readonly Dictionary<string, string[]> LAST_CONSONANTS = new Dictionary<string, string[]>(){
                 //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
                {"ㄱ", new string[2]{"k", ""}},
                {"ㄲ", new string[2]{"k", ""}},
                {"ㄳ", new string[2]{"k", ""}},
                {"ㄴ", new string[2]{"n", "2"}},
                {"ㄵ", new string[2]{"n", "2"}},
                {"ㄶ", new string[2]{"n", "2"}},
                {"ㄷ", new string[2]{"t", "1"}},
                {"ㄹ", new string[2]{"l", "4"}},
                {"ㄺ", new string[2]{"k", ""}},
                {"ㄻ", new string[2]{"m", "1"}},
                {"ㄼ", new string[2]{"l", "4"}},
                {"ㄽ", new string[2]{"l", "4"}},
                {"ㄾ", new string[2]{"l", "4"}},
                {"ㄿ", new string[2]{"p", "1"}},
                {"ㅀ", new string[2]{"l", "4"}},
                {"ㅁ", new string[2]{"m", "1"}},
                {"ㅂ", new string[2]{"p", "1"}},
                {"ㅄ", new string[2]{"p", "1"}},
                {"ㅅ", new string[2]{"t", "1"}},
                {"ㅆ", new string[2]{"t", "1"}},
                {"ㅇ", new string[2]{"ng", "3"}},
                {"ㅈ", new string[2]{"t", "1"}},
                {"ㅊ", new string[2]{"t", "1"}},
                {"ㅋ", new string[2]{"k", ""}},
                {"ㅌ", new string[2]{"t", "1"}},
                {"ㅍ", new string[2]{"p", "1"}},
                {"ㅎ", new string[2]{"t", "1"}},
                {" ", new string[2]{"", ""}},
                {"null", new string[2]{"", ""}} // 뒤 글자가 없을 때를 대비
                };


            private Hanguel hanguel = new Hanguel();

            public CV() { }
            private Hashtable convertForCV(Hashtable separated, bool[] setting) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                Hashtable separatedConvertedForCV;

                separatedConvertedForCV = new Hashtable() {
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0],
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1],
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2],
                    [3] = LAST_CONSONANTS[(string)separated[2]][0],

                    [4] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [5] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [7] = LAST_CONSONANTS[(string)separated[5]][0],

                    [8] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [9] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [10] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [11] = LAST_CONSONANTS[(string)separated[8]][0]
                };

                if ((setting[0]) && (separatedConvertedForCV[4].Equals("s")) && (separatedConvertedForCV[6].Equals("i"))) {
                    // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                    separatedConvertedForCV[4] = "sh"; // si to shi
                } else if ((!setting[2]) && (separated[4].Equals("ㅢ"))) {
                    // isUsingShi, isUsing_aX, [isUsing_i], isRentan
                    separatedConvertedForCV[5] = "eu"; // to eui
                }

                return separatedConvertedForCV;
            }

            private Hashtable convertForCVSingle(Hashtable separated, bool[] setting) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                // 한 글자짜리 노트 받아서 반환함 (숨소리 생성용)
                Hashtable separatedConvertedForCV;

                separatedConvertedForCV = new Hashtable() {
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], // n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                };

                if ((setting[0]) && (separatedConvertedForCV[0].Equals("s")) && (separatedConvertedForCV[2].Equals("i"))) {
                    // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                    separatedConvertedForCV[0] = "sh"; // si to shi
                } else if ((!setting[2]) && (separated[1].Equals("ㅢ"))) {
                    // isUsingShi, isUsing_aX, [isUsing_i], isRentan
                    separatedConvertedForCV[2] = "eu"; // to eui
                }

                return separatedConvertedForCV;
            }

            public Hashtable convertForCV(Note? prevNeighbour, Note note, Note? nextNeighbour, bool[] setting) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCV(hanguel.variate(prevNeighbour, note, nextNeighbour), setting);

            }

            public Hashtable convertForCV(Note? prevNeighbour, bool[] setting) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCVSingle(hanguel.variate(prevNeighbour?.lyric), setting);

            }

        }

        public override Result convertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cvPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120; // TODO
            int vcLengthShort = 30;

            Hanguel hanguel = new Hanguel();
            CV cv = new CV();

            try {
                cvPhonemes = cv.convertForCV(prevNote, thisNote, nextNote, new bool[] { isUsingShi, isUsing_aX, isUsing_i, isRentan }); // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                                                                                                                                        // 음운변동이 진행됨 => 위에서 반환된 음소로 전부 때울 예정
            } catch {
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = lyric},
                            }
                };
            }


            // ex 냥냐 (nya3 ang nya)
            string thisFirstConsonant = (string)cvPhonemes[4]; // n
            string thisVowelHead = (string)cvPhonemes[5]; // y
            string thisVowelTail = (string)cvPhonemes[6]; // a
            string thisLastConsonant = (string)cvPhonemes[7]; // ng

            string nextFirstConsonant; // VC음소 만들 때 쓰는 다음 노트의 자음 음소 / CVC 음소와는 관계 없음 // ny
            string nextVowelHead = (string)cvPhonemes[9]; // 다음 노트 모음의 머리 음소 / y

            string prevVowelTail = (string)cvPhonemes[2]; // VV음소 만들 때 쓰는 이전 노트의 모음 음소 / CV, CVC 음소와는 관계 없음 // a
            string prevLastConsonant = (string)cvPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소

            string VV = $"{thisVowelTail}"; // a
            string CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}"; // nya4

            string cVC;
            string frontCV; // 연단음일 때엔 -가 붙은 형식으로 저장되는 변수 
            string? endSoundVowel = findInOto($"{thisVowelTail} -", note, true);
            string? endSoundLastConsonant = findInOto($"{thisLastConsonant} -", note, true);

            int cVCLength; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용

            if (thisLastConsonant.Equals("l")) {
                // ㄹ받침
                cVCLength = totalDuration / 2;
            } else if (thisLastConsonant.Equals("n")) {
                // ㄴ받침
                cVCLength = 170;
            } else if (thisLastConsonant.Equals("ng")) {
                // ㅇ받침
                cVCLength = 230;
            } else if (thisLastConsonant.Equals("m")) {
                // ㅁ받침
                cVCLength = 280;
            } else if (thisLastConsonant.Equals("k")) {
                // ㄱ받침
                cVCLength = totalDuration / 2;
            } else if (thisLastConsonant.Equals("t")) {
                // ㄷ받침
                cVCLength = totalDuration / 2;
            } else if (thisLastConsonant.Equals("p")) {
                cVCLength = totalDuration / 2;
            } else {
                // 나머지
                cVCLength = totalDuration / 3;
            }

            if (thisVowelTail.Equals("u")) {
                cVCLength += 50; // 모음이 u일때엔 cVC의 발음 길이가 더 길어짐
                vcLength += 50;
            }

            if (isUsing_aX) {
                // 받침 음소를 aX 형식으로 사용
                cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 
            } else {
                // 받침 음소를 a X 형식으로 사용
                cVC = $"{thisVowelTail} {thisLastConsonant}"; // a ng 
            }


            if (!isUsing_i) {
                // ㅢ를 ㅣ로 대체해서 발음하지 않을 때
                if (singer.TryGetMappedOto($"{CV}", thisNote.tone, out UOto oto)) {
                    // (consonant)eui 있는지 체크
                    CV = $"{CV}";
                } else {
                    // (consonant)eui 없으면 i 사용
                    CV = $"{thisFirstConsonant}{thisVowelTail}";
                }

            }

            if (isRentan) {
                // 연단음 / 어두 음소(-) 사용 
                if (findInOto($"- {CV}", note, true) == null) {
                    if (findInOto($"-{CV}", note, true) == null) {
                        frontCV = findInOto($"-{CV}", note, true);
                        CV = findInOto($"{CV}", note);
                    }
                    frontCV = findInOto($"-{CV}", note, true);
                    CV = findInOto($"{CV}", note);
                } else {
                    CV = findInOto($"{CV}", note);
                    frontCV = CV;
                }

            } else {
                // 연단음 아님 / 어두 음소(-) 미사용
                CV = findInOto($"{CV}", note);
                frontCV = CV;

            }


            if ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("eu"))) {
                nextFirstConsonant = $"{(string)cvPhonemes[8]}"; // VC에 썼을 때 eu bw 대신 eu b를 만들기 위함
            } else if ((nextVowelHead.Equals("y") && (thisVowelTail.Equals("i")))) {
                nextFirstConsonant = $"{(string)cvPhonemes[8]}"; // VC에 썼을 때 i by 대신 i b를 만들기 위함
            } else {
                nextFirstConsonant = $"{(string)cvPhonemes[8]}{(string)cvPhonemes[9]}"; // 나머지... ex) ny
            }

            string VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VC, CVC에게는 해당 없음


            VC = findInOto(VC, note);
            VV = findInOto(VV, note);
            cVC = findInOto(cVC, note);
            if (endSoundVowel == null) {
                endSoundVowel = "";
            }
            if (endSoundLastConsonant == null) {
                endSoundLastConsonant = "";
            }

            if (frontCV == null) {
                frontCV = CV;
            }
            // return phonemes
            if ((prevNeighbour == null) && (nextNeighbour == null)) {
                // 이웃이 없음 / 냥

                if (thisLastConsonant.Equals("")) { // 이웃 없고 받침 없음 / 냐
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                    };
                } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))) {
                    // 이웃 없고 받침 있음 - ㄴㄹㅇㅁ / 냥
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                    };
                } else {
                    // 이웃 없고 받침 있음 - 나머지 / 냑
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                    };
                }
            } else if ((prevNeighbour != null) && (nextNeighbour == null)) {
                // 앞에 이웃 있고 뒤에 이웃 없음 / 냥[냥]
                if (thisLastConsonant.Equals("")) { // 뒤이웃만 없고 받침 없음 / 냐[냐]
                    if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                        // 앞에 받침 없는 모음 / 냐[아]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                        };
                    } else if ((!prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (!thisVowelTail.Equals(""))) {
                        // 앞에 받침이 온 모음 / 냥[아]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)}
                            }
                        };
                    } else {
                        // 모음아님 / 냐[냐]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                        };
                    }

                } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))) {
                    // 뒤이웃만 없고 받침 있음 - ㄴㄹㅇㅁ  / 냐[냥]

                    if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                        // 앞에 받침 없는 모음 / 냐[앙]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    } else if ((!prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (!thisVowelTail.Equals(""))) {
                        // 앞에 받침이 온 모음 / 냥[앙]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    } else {
                        // 모음 아님 / 냥[냥]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }

                } else {
                    // 뒤이웃만 없고 받침 있음 - 나머지 / 냐[냑]
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                    };
                }


            } else if ((prevNeighbour == null) && (nextNeighbour != null)) {
                if (hanguel.isHangeul(nextNeighbour?.lyric)) {
                    // 뒤 글자가 한글임
                    // 앞에 이웃 없고 뒤에 있음

                    if (thisLastConsonant.Equals("")) { // 앞이웃만 없고 받침 없음 / [냐]냥
                        if (nextFirstConsonant.Equals("")) {
                            // 뒤에 VV 와야해서 VC 오면 안됨
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                            };
                        } else {
                            if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))) {
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                                };
                            } else {
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                                };
                            }
                        }

                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))) {
                        // 앞이웃만 없고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / [냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("ng")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("jw"))) {
                            return new Result() {
                                phonemes = new Phoneme[] {
                            // 다음 음소가 ㄴㅇㄹㅇ ㄱㄷㅂ 임 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }// -음소 없이 이어줌
                            };
                        } else {
                            // 다음 음소가 나머지임
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }// -음소 있이 이어줌
                            };
                        }

                    } else {
                        // 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                } else if (nextNeighbour?.lyric == "-") {
                    if (thisLastConsonant.Equals("")) {
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                        };
                    } else {
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                } else {
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                    };
                }
            } else if ((prevNeighbour != null) && (nextNeighbour != null)) {
                // 둘다 이웃 있음
                if (hanguel.isHangeul(nextNeighbour?.lyric)) {
                    // 뒤의 이웃이 한국어임

                    if (thisLastConsonant.Equals("")) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals("")) && (nextFirstConsonant.Equals(""))) {
                            // 앞에 받침 없는 모음 / 뒤에 모음 옴 / 냐[아]아
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            }
                            };
                        } else if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 뒤에 자음 옴 / 냐[아]냐
                            if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))) {
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"",
                            position = totalDuration - totalDuration / 2},
                            }
                                };
                            } else {
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            }
                                };
                            }

                        } else {
                            // 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            if (nextFirstConsonant.Equals("")) {
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                                };
                            } else if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))) {
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = "",
                            position = totalDuration - totalDuration / 2},
                            }
                                };
                            } else {
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                                };
                            }

                        }

                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))) {
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("jw"))) {
                            // 다음 음소가 ㄴㅇㄹㅇ ㄱㄷㅂㅅㅈ 임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없는 모음 / 냐[앙]냐
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                                };
                            } else {
                                // 앞에 받침이 있는 모음 + 모음 아님 / 냥[앙]냐 냥[냥]냥
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }// -음소 없이 이어줌
                                };
                            }

                        } else {
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없는 모음 / 냐[앙]꺅
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)}
                            }
                                };
                            } else {
                                // 앞에 받침 있는 모음 + 모음 아님 / 냥[앙]꺅  냥[냥]꺅
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                        }// -음소 있이 이어줌
                                };
                            }

                        }

                    } else {
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)}
                            }
                            };
                        } else {
                            // 앞에 받침이 온 모음 + 모음 아님  냥[악]꺅  냥[먁]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            }
                            };
                        }

                    }
                } else if ((nextNeighbour?.lyric == "-") || (nextNeighbour?.lyric == "R")) {
                    // 둘다 이웃 있고 뒤에 -가 옴
                    if (thisLastConsonant.Equals("")) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 냐[아]냐
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            }
                            };
                        } else {
                            // 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                            };
                        }

                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))) {
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("m"))) {
                            // 다음 음소가 ㄴㅇㄹㅇ 임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없는 모음 / 냐[앙]냐
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                                };
                            } else {
                                // 앞에 받침이 있는 모음 + 모음 아님 / 냥[앙]냐 냥[냥]냥
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }// -음소 없이 이어줌
                                };
                            }

                        } else {
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없는 모음 / 냐[앙]꺅
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                                };
                            } else {
                                // 앞에 받침 있는 모음 + 모음 아님 / 냥[앙]꺅  냥[냥]꺅
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                                };
                            }

                        }

                    } else {
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                            };
                        } else {
                            // 앞에 받침이 온 모음 + 모음 아님  냥[악]꺅  냥[먁]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                            };
                        }

                    }
                } else {
                    if (thisLastConsonant.Equals("")) { // 둘다 이웃 있고 받침 없음 / 냥[냐]-
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                        }
                        };
                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))) {
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]-

                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                                }// -음소 없이 이어줌
                        };
                    } else {
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]-
                        return new Result() {
                            phonemes = new Phoneme[] {
                                new Phoneme { phoneme = $"{CV}"},
                                new Phoneme { phoneme = $"{cVC}",
                                position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                                }
                        };
                    }
                }
            } else {
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme {phoneme = CV},
                            }
                };

            }
        }

        public override Result generateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cvPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);

            Hanguel hanguel = new Hanguel();
            CV cv = new CV();
            string phonemeToReturn = lyric; // 아래에서 아무것도 안 걸리면 그냥 가사 반환
            string prevLyric = prevNote?.lyric;

            if (phonemeToReturn.Equals("-")) {
                if (hanguel.isHangeul(prevLyric)) {
                    cvPhonemes = cv.convertForCV(prevNote, new bool[] { isUsingShi, isUsing_aX, isUsing_i, isRentan }); // [isUsingShi], isUsing_aX, isUsing_i, isRentan

                    string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} -";
                    } else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} -";
                    }

                }
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
            } else if (phonemeToReturn.Equals("R")) {
                if (hanguel.isHangeul(prevLyric)) {
                    cvPhonemes = cv.convertForCV(prevNote, new bool[] { isUsingShi, isUsing_aX, isUsing_i, isRentan }); // [isUsingShi], isUsing_aX, isUsing_i, isRentan

                    string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} R";
                    } else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} R";
                    }

                }
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
            } else {
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
            }
        }
    }
}