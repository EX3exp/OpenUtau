using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;

using OpenUtau.Core.Ustx;

// TODO: refactoring code
namespace OpenUtau.Plugin.Builtin {
    /// Phonemizer for 'KOR CBNN(Combination)' ///
    [Phonemizer("Korean CBNN Phonemizer", "KO CBNN", "EX3", language: "KO")]

    public class KoreanCBNNPhonemizer : Core.BaseKoreanPhonemizer {
        private class CBNN {
            public Hanguel hangeul = new Hanguel();

            /// <summary>
            /// CBNN phoneme table of first consonants. (key "null" is for Handling empty string)
            /// </summary>
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

            /// <summary>
            /// CBNN phoneme table of middle vowels (key "null" is for Handling empty string)
            /// </summary>
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
                {"ㅚ", new string[3]{"we", "w", "e"}},
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

            /// <summary>
            /// CBNN phoneme table of last consonants. (key "null" is for Handling empty string)
            /// </summary>
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


            public CBNN() { }

            /// <summary>
            /// Converts result of Hangeul.variate(Note? prevNeighbour, Note note, Note? nextNeighbour) into CBNN format.
            /// <br/>Hangeul.variate(Note? prevNeighbour, Note note, Note? nextNeighbour)를 사용한 결과물을 받아 CBNN식으로 변경합니다.
            /// </summary>
            /// <param name="separated">
            /// result of Hangeul.variate(Note? prevNeighbour, Note note, Note? nextNeighbour).
            /// </param>
            /// <returns>
            /// Returns CBNN formated result. 
            /// </returns>
            private Hashtable convertForCBNN(Hashtable separated) {
                // VV 음소를 위해 앞의 노트의 변동된 결과까지 반환한다
                // vc 음소를 위해 뒤의 노트의 변동된 결과까지 반환한다
                Hashtable separatedConvertedForCBNN;

                separatedConvertedForCBNN = new Hashtable() {
                    // first character
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                    // second character
                    [5] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [7] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [8] = LAST_CONSONANTS[(string)separated[5]][1],
                    [9] = LAST_CONSONANTS[(string)separated[5]][0],

                    // last character
                    [10] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [11] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [12] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [13] = LAST_CONSONANTS[(string)separated[8]][1],
                    [14] = LAST_CONSONANTS[(string)separated[8]][0]
                };

                return separatedConvertedForCBNN;
            }

            /// <summary>
            /// Converts result of Hangeul.variate(charcter) into CBNN format.
            /// <br/>Hangeul.variate(character)를 사용한 결과물을 받아 CBNN식으로 변경합니다.
            /// </summary>
            /// <param name="separated">
            /// result of Hangeul.variate(Note? prevNeighbour, Note note, Note? nextNeighbour).
            /// </param>
            /// <returns>
            /// Returns CBNN formated result. 
            /// </returns>
            private Hashtable convertForCBNNSingle(Hashtable separated) {
                // inputs and returns only one character. (한 글자짜리 인풋만 받음)
                Hashtable separatedConvertedForCBNN;

                separatedConvertedForCBNN = new Hashtable() {
                    // first character
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                };

                return separatedConvertedForCBNN;
            }


            /// <summary>
            /// Conducts phoneme variation automatically with prevNeighbour, note, nextNeighbour, in CBNN format.  
            /// <br/><br/> prevNeighbour, note, nextNeighbour를 입력받아 자동으로 음운 변동을 진행하고, 결과물을 CBNN 식으로 변경합니다.
            /// </summary>
            /// <param name="prevNeighbour"> Note of prev note, if exists(otherwise null).
            /// <br/> 이전 노트 혹은 null.
            /// <br/><br/>(Example: Note with lyric '춘')
            /// </param>
            /// <param name="note"> Note of current note. 
            /// <br/> 현재 노트.
            /// <br/><br/>(Example: Note with lyric '향')
            /// </param>
            /// <param name="nextNeighbour"> Note of next note, if exists(otherwise null).
            /// <br/> 다음 노트 혹은 null.
            /// <br/><br/>(Example: null)
            /// </param>
            /// <returns> Returns phoneme variation result of prevNote, currentNote, nextNote.
            /// <br/>이전 노트, 현재 노트, 다음 노트의 음운변동 결과를 CBNN 식으로 변환해 반환합니다.
            /// <br/>Example: 춘 [향] null: {[0]="ch", [1]="", [1]="u", [3]="", [4]="", 
            /// <br/>[5]="n", [6]="y", [7]="a", [8]="2", [9]="ng", 
            /// <br/>[10]="", [11]="", [12]="", [13]="", [14]=""} [추 냥 null]
            /// </returns>
            public Hashtable convertForCBNN(Note? prevNeighbour, Note note, Note? nextNeighbour) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCBNN(hangeul.variate(prevNeighbour, note, nextNeighbour));

            }

            public Hashtable convertForCBNN(Note? prevNeighbour) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCBNNSingle(hangeul.variate(prevNeighbour?.lyric));

            }

        }
        public override Result convertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cbnnPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120; // TODO
            int vcLengthShort = 90;
            Hanguel hanguel = new Hanguel();
            CBNN CBNN = new CBNN();

            try {
                // change lyric to CBNN phonemes, with phoneme variation.
                cbnnPhonemes = CBNN.convertForCBNN(prevNote, thisNote, nextNote);
            } catch {
                return new Result() {
                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = lyric},
                            }
                };
            }

            // ex 냥냐 (nya3 ang nya)
            string thisFirstConsonant = (string)cbnnPhonemes[5]; // n
            string thisVowelHead = (string)cbnnPhonemes[6]; // y
            string thisVowelTail = (string)cbnnPhonemes[7]; // a
            string thisSuffix = (string)cbnnPhonemes[8]; // 3
            string thisLastConsonant = (string)cbnnPhonemes[9]; // ng

            string nextFirstConsonant; // VC음소 만들 때 쓰는 다음 노트의 자음 음소 / CVC 음소와는 관계 없음 // ny
            string nextVowelHead = (string)cbnnPhonemes[11]; // 다음 노트 모음의 머리 음소 / y

            string prevVowelTail = (string)cbnnPhonemes[2]; // VV음소 만들 때 쓰는 이전 노트의 모음 음소 / CV, CVC 음소와는 관계 없음 // a
            string prevLastConsonant = (string)cbnnPhonemes[4]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소
            string prevSuffix = (string)cbnnPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 접미사 / 3

            string VV = $"{prevVowelTail} {thisVowelTail}"; // i a
            string CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}{thisSuffix}"; // nya4
            string frontCV = $"- {CV}";
            string cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 

            string endSoundVowel = $"{thisVowelTail} -"; // a -
            string endSoundLastConsonant = $"{thisLastConsonant} -"; // ng -

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


            if (((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("eu"))) || ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("o"))) || ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("u")))) {
                nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 eu bw 대신 eu b를 만들기 위함
            } else if (((nextVowelHead.Equals("y") && (thisVowelTail.Equals("i")))) || ((nextVowelHead.Equals("y")) && (thisVowelTail.Equals("eu")))) {
                nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 i by 대신 i b를 만들기 위함
            } else {
                nextFirstConsonant = $"{(string)cbnnPhonemes[10]}{(string)cbnnPhonemes[11]}"; // 나머지... ex) ny
            }

            string VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VV, CVC에게는 해당 없음


            if (!thisSuffix.Equals("")) {
                // 접미사가 있는 발음일 때 / nya2
                if (singer.TryGetMappedOto($"{CV}", thisNote.tone, out UOto oto)) {
                    // 해당 발음 있는지 체크
                    CV = $"{CV}";
                } else {
                    // 없으면 접미사 없는 발음 사용 / nya
                    CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}";
                }

            }

            // set Voice color & Tone

            frontCV = findInOto(frontCV, note, true);
            CV = findInOto(CV, note);
            VC = findInOto(VC, note, true);
            VV = findInOto(VV, note, true);
            cVC = findInOto(cVC, note);
            endSoundVowel = findInOto(endSoundVowel, note);
            endSoundLastConsonant = findInOto(endSoundLastConsonant, note);

            if (frontCV == null) {
                frontCV = CV;
            }
            if (VV == null) {
                // VV음소 없으면 (ex : a i) 대응하는 CV음소 사용 (ex:  i)
                VV = CV;
            }

            // Return phonemes
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
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
                            }
                    };
                } else {
                    // 이웃 없고 받침 있음 - 나머지 / 냑
                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
                            }
                    };
                }
            } else if ((prevNeighbour != null) && (nextNeighbour == null)) {
                // 앞에 이웃 있고 뒤에 이웃 없음 / 냥[냥]
                if (thisLastConsonant.Equals("")) { // 뒤이웃만 없고 받침 없음 / 냐[냐]
                    if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                        // 앞에 받침 없는 모음 / 냐[아]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                        };
                    } else if ((prevSuffix.Equals("")) && (!prevLastConsonant.Equals("")) && (thisSuffix.Equals("")) && (thisFirstConsonant.Equals("")) && (!thisVowelTail.Equals(""))) {
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
                        if ((thisFirstConsonant.Equals("g")) || (thisFirstConsonant.Equals("d")) || (thisFirstConsonant.Equals("b")) || (thisFirstConsonant.Equals("s")) || (thisFirstConsonant.Equals("j")) || (thisFirstConsonant.Equals("n")) || (thisFirstConsonant.Equals("r")) || (thisFirstConsonant.Equals("m")) || (thisFirstConsonant.Equals("")) || (prevLastConsonant.Equals(""))) {
                            // ㄱㄷㅂㅅㅈㄴㄹㅁ일 경우 CV로 이음
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                            };
                        } else {
                            // 이외 음소는 - CV로 이음
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                            };
                        }

                    }

                } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))) {
                    // 뒤이웃만 없고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / 냐[냥]

                    if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                        // 앞에 받침 없는 모음 / 냐[앙]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    } else if ((prevSuffix.Equals("")) && (!prevLastConsonant.Equals("")) && (thisSuffix.Equals("")) && (thisFirstConsonant.Equals("")) && (!thisVowelTail.Equals(""))) {
                        // 앞에 받침이 온 모음 / 냥[앙]
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength),},
                            }
                        };
                    } else {
                        // 뒤이웃만 없고 받침 있음 - 나머지 / 냐[냑]
                        if ((thisFirstConsonant.Equals("g")) || (thisFirstConsonant.Equals("d")) || (thisFirstConsonant.Equals("b")) || (thisFirstConsonant.Equals("s")) || (thisFirstConsonant.Equals("j")) || (thisFirstConsonant.Equals("n")) || (thisFirstConsonant.Equals("r")) || (thisFirstConsonant.Equals("m")) || (thisFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("s")) || (prevLastConsonant.Equals(""))) {
                            // 앞받침 있고 다음이 ㄱㄷㅂㅅㅈㄴㄹㅁ일 경우 CV로 이음
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
                            }
                            };
                        } else {
                            // 이외 음소는 - CV로 이음
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                            };
                        }
                    }

                } else {

                    return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
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
                        } else if (VC != null) {
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                            };
                        } else {
                            // 음원에 VC 존재하지 않음
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                            };
                        }

                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))) {
                        // 앞이웃만 없고 받침 있음 - ㄴㄹㅇㅁ / [냥]냐
                        if ((nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("s") || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("ny")) || (nextFirstConsonant.Equals("ry")) || (nextFirstConsonant.Equals("my")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("jw")) || (nextFirstConsonant.Equals("nw")) || (nextFirstConsonant.Equals("rw")) || (nextFirstConsonant.Equals("mw")) || (nextFirstConsonant.Equals("sw")))) {
                            return new Result() {
                                phonemes = new Phoneme[] {
                            // 다음 음소가 ㄴㅇㄹㅁㄱㄷㅂ 임 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            }// -음소 없이 이어줌
                            };
                        } else {
                            // 다음 음소가 나머지임
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2},
                            }// -음소 있이 이어줌
                            };
                        }

                    } else {
                        // 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return new Result() {
                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
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
                            position = totalDuration - Math.Min(totalDuration / 3, cVCLength)},
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
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals("")) && (nextFirstConsonant.Equals(""))) {
                            // 앞에 받침 없는 모음 / 뒤에 모음 옴 / 냐[아]아
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            }
                            };
                        } else if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 뒤에 자음 옴 / 냐[아]냐
                            if (VC != null) {
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                                };
                            } else {
                                // 음원에 VC 존재하지 않음
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            }
                                };
                            }

                        } else {
                            // 앞에 받침 있고 뒤에 모음 옴 / 냐[냐]아  냥[아]아
                            if (nextFirstConsonant.Equals("")) {
                                if ((!prevLastConsonant.Equals("")) && ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("ch")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p")))) {
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                                    };
                                } else {
                                    // 나머지 / CV로 시작
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                                    };
                                }
                            } else {
                                // 앞에 받침 있고 뒤에 모음 안옴
                                if ((!prevLastConsonant.Equals("")) && ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("ch")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p")))) {
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    if (VC != null) {
                                        return new Result() {
                                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                                        };
                                    } else {
                                        // 음원에 VC 존재하지 않음
                                        return new Result() {
                                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                                        };
                                    }

                                } else {
                                    // 나머지 / CV로 시작
                                    if (VC != null) {
                                        return new Result() {
                                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                                        };
                                    } else {
                                        // 음원에 VC 존재하지 않음
                                        return new Result() {
                                            phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            }
                                        };
                                    }


                                }


                            }



                        }

                    } else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))) {
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ인 아무런 받침 / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("s") || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("ny")) || (nextFirstConsonant.Equals("ry")) || (nextFirstConsonant.Equals("my")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("jw")) || (nextFirstConsonant.Equals("nw")) || (nextFirstConsonant.Equals("rw")) || (nextFirstConsonant.Equals("mw")) || (nextFirstConsonant.Equals("sw")))) {
                            // 다음 음소가 ㄱㄷㅂㅅㅈㄴㅇㄹㅇ 임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없고 받침 있는 모음 / 냐[앙]냐
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            }
                                };
                            } else {
                                // 앞에 받침 있고 받침 오는 CV / 냥[냥]냐 
                                if (((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("ch")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))) && (!prevLastConsonant.Equals(""))) {
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                                    };
                                } else {
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }// -음소 없이 이어줌
                                    };
                                }

                            }

                        } else {
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                                // 앞에 받침 없는 모음 / 냐[앙]꺅
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2}
                            }
                                };
                            } else {
                                // 앞에 받침 있고 받침 있는 CVC / 냥[냥]꺅
                                if ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("ch")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))) {
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2},
                        }
                                    };
                                } else {
                                    // 나머지 음소 
                                    return new Result() {
                                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2},
                        }
                                    };
                                }

                            }

                        }

                    } else {
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2}
                            }
                            };
                        } else {
                            // 앞에 받침이 온 CVC 음소(받침 있음) / 냥[악]꺅  냥[먁]꺅
                            if ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("ch")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))) {
                                // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2}
                            }
                                };
                            } else {
                                // 나머지
                                return new Result() {
                                    phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - totalDuration / 2}
                            }
                                };
                            }
                        }



                    }
                } else if ((nextNeighbour?.lyric == "-") || (nextNeighbour?.lyric == "R")) {
                    // 둘다 이웃 있고 뒤에 -가 옴
                    if (thisLastConsonant.Equals("")) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
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
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
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
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
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
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))) {
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
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
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                                }// -음소 없이 이어줌
                        };
                    } else {
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]-
                        return new Result() {
                            phonemes = new Phoneme[] {
                                new Phoneme { phoneme = $"{CV}"},
                                new Phoneme { phoneme = $"{cVC}",
                                position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
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
            Hashtable cbnnPhonemes;


            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120; // TODO
            int vcLengthShort = 90;
            Hanguel hanguel = new Hanguel();
            CBNN CBNN = new CBNN();
            string phonemeToReturn = lyric; // 아래에서 아무것도 안 걸리면 그냥 가사 반환
            string prevLyric = prevNote?.lyric;

            if (thisNote.lyric.Equals("-")) {
                if (hanguel.isHangeul(prevLyric)) {
                    cbnnPhonemes = CBNN.convertForCBNN(prevNote);

                    string prevVowelTail = (string)cbnnPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cbnnPhonemes[4]; // 이전 노트의 받침 음소

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
            } else if (thisNote.lyric.Equals("R")) {
                if (hanguel.isHangeul(prevLyric)) {
                    cbnnPhonemes = CBNN.convertForCBNN(prevNote);

                    string prevVowelTail = (string)cbnnPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cbnnPhonemes[4]; // 이전 노트의 받침 음소

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
