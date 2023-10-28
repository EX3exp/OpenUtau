using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

// TODO: refactoring code
namespace OpenUtau.Plugin.Builtin {
    /// Phonemizer for 'KOR CBNN(Combination)' ///
    

    [Phonemizer("Korean CBNN Phonemizer", "KO CBNN", "EX3", language: "KO")]

    
    public class KoreanCBNNPhonemizer : Core.BaseKoreanPhonemizer {
        private class CBNN {
             /// <summary>
        /// First Consonant's type.
        /// </summary>
            public enum ConsonantType{
       
        /// 
        /// <summary>예사소리</summary>
        NORMAL, 
        /// <summary>거센소리</summary>
        ASPIRATE, 
        /// <summary>된소리</summary>
        FORTIS, 
        /// <summary>마찰음</summary>
        FRICATIVE, 
        /// <summary>비음</summary>
        NASAL,
        /// <summary>유음</summary>
        LIQUID, 
        /// <summary>ㅎ</summary>
        H,
        /// <summary>자음의 음소값 없음(ㅇ)</summary>
        NOCONSONANT, 
        /// <summary>음소 자체가 없음</summary>
        PHONEME_IS_NULL
    }

    /// <summary>
        /// Last Consonant's type.
        /// </summary>
    public enum BatchimType{
       
        /// 
        /// <summary>예사소리 받침</summary>
        NORMAL_END, 
        /// <summary>비음 받침</summary>
        NASAL_END,
        /// <summary>유음 받침</summary>
        LIQUID_END, 
        /// <summary>ㅇ받침</summary>
        NG_END, 
        /// <summary>ㅎ받침</summary>
        H_END,
        /// <summary>받침이 없음</summary>
        NO_END,
        /// <summary>음소 자체가 없음</summary>
        PHONEME_IS_NULL
    }
            public Hanguel hangeul = new Hanguel();

            /// <summary>
            /// CBNN phoneme table of first consonants. (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> FIRST_CONSONANTS = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g", ConsonantType.NORMAL.ToString()}},
                {"ㄲ", new string[2]{"gg", ConsonantType.FORTIS.ToString()}},
                {"ㄴ", new string[2]{"n", ConsonantType.NASAL.ToString()}},
                {"ㄷ", new string[2]{"d", ConsonantType.NORMAL.ToString()}},
                {"ㄸ", new string[2]{"dd", ConsonantType.FORTIS.ToString()}},
                {"ㄹ", new string[2]{"r", ConsonantType.LIQUID.ToString()}},
                {"ㅁ", new string[2]{"m", ConsonantType.NASAL.ToString()}},
                {"ㅂ", new string[2]{"b", ConsonantType.NORMAL.ToString()}},
                {"ㅃ", new string[2]{"bb", ConsonantType.FORTIS.ToString()}},
                {"ㅅ", new string[2]{"s", ConsonantType.NORMAL.ToString()}},
                {"ㅆ", new string[2]{"ss", ConsonantType.FRICATIVE.ToString()}},
                {"ㅇ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"ㅈ", new string[2]{"j", ConsonantType.NORMAL.ToString()}},
                {"ㅉ", new string[2]{"jj", ConsonantType.FORTIS.ToString()}},
                {"ㅊ", new string[2]{"ch", ConsonantType.ASPIRATE.ToString()}},
                {"ㅋ", new string[2]{"k", ConsonantType.ASPIRATE.ToString()}},
                {"ㅌ", new string[2]{"t", ConsonantType.ASPIRATE.ToString()}},
                {"ㅍ", new string[2]{"p", ConsonantType.ASPIRATE.ToString()}},
                {"ㅎ", new string[2]{"h", ConsonantType.H.ToString()}},
                {" ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"null", new string[2]{"", ConsonantType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
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
                {"ㄱ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄲ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄳ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄴ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄵ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄶ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄷ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㄹ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄺ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄻ", new string[3]{"m", "1", BatchimType.NASAL_END.ToString()}},
                {"ㄼ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄽ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄾ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄿ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅀ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㅁ", new string[3]{"m", "1", BatchimType.NASAL_END.ToString()}},
                {"ㅂ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅄ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅅ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅆ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅇ", new string[3]{"ng", "3", BatchimType.NG_END.ToString()}},
                {"ㅈ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅊ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅋ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㅌ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅍ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅎ", new string[3]{"t", "1", BatchimType.H_END.ToString()}},
                {" ", new string[3]{"", "", BatchimType.NO_END.ToString()}},
                {"null", new string[3]{"", "", BatchimType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };

            private string thisFirstConsonant, thisVowelHead, thisVowelTail, thisSuffix, thisLastConsonant;
            private string nextFirstConsonant, nextVowelHead, nextLastConsonant;
            private string prevVowelTail, prevLastConsonant, prevSuffix, prevVowelHead; 

            public string VV, CV, cVC, VC, CV_noSuffix; 
            public string frontCV, frontCV_noSuffix; // - {CV}
            public string endSoundVowel, endSoundLastConsonant; // ng -
            public int cVCLength, vcLength, vcLengthShort; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용
            private int totalDuration;

            private ConsonantType thisFirstConsonantType, prevFirstConsonantType, nextFirstConsonantType;
            private BatchimType thisLastConsonantType, prevLastConsonantType, nextLastConsonantType;
            private Note note;
            private USinger singer;
            public CBNN(USinger singer, Note note, int totalDuration, int vcLength = 120, int vcLengthShort = 90) {
                this.totalDuration = totalDuration;
                this.vcLength = vcLength;
                this.vcLengthShort = vcLengthShort;
                this.singer = singer;
                this.note = note;
            }

            private string? findInOto(String phoneme, Note note, bool nullIfNotFound=false){
                return BaseKoreanPhonemizer.findInOto(singer, phoneme, note, nullIfNotFound);
            }

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
                Hashtable cbnnPhonemes;

                cbnnPhonemes = new Hashtable() {
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

                // ex 냥냐 (nya3 ang nya)
                thisFirstConsonant = (string)cbnnPhonemes[5]; // n
                thisVowelHead = (string)cbnnPhonemes[6]; // y
                thisVowelTail = (string)cbnnPhonemes[7]; // a
                thisSuffix = (string)cbnnPhonemes[8]; // 3
                thisLastConsonant = (string)cbnnPhonemes[9]; // ng

                nextVowelHead = (string)cbnnPhonemes[11]; // 다음 노트 모음의 머리 음소 / y
                nextLastConsonant = (string)cbnnPhonemes[14];

                prevVowelHead = (string)cbnnPhonemes[1];
                prevVowelTail = (string)cbnnPhonemes[2]; // VV음소 만들 때 쓰는 이전 노트의 모음 음소 / CV, CVC 음소와는 관계 없음 // a
                prevLastConsonant = (string)cbnnPhonemes[4]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소
                prevSuffix = (string)cbnnPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 접미사 / 3

                VV = $"{prevVowelTail} {thisVowelTail}"; // i a
                CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}{thisSuffix}"; // nya4
                frontCV = $"- {CV}"; // - nya4
                CV_noSuffix = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}"; // nya
                frontCV_noSuffix = $"- {CV_noSuffix}"; // - nya
                cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 

                endSoundVowel = $"{thisVowelTail} -"; // a -
                endSoundLastConsonant = $"{thisLastConsonant} -"; // ng -

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

            VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VV, CVC에게는 해당 없음


            
            // set Voice color & Tone

            frontCV = findInOto(frontCV, note, true);

            if (!thisSuffix.Equals("")) {
                // 접미사가 있는 발음일 때 / nya2
                if (!singer.TryGetMappedOto($"{CV}", note.tone, out UOto oto)) {
                    CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}";
                }

            }
            if (thisSuffix.Equals("")){
                CV = findInOto(CV, note);
            }
            else{
                CV = findInOto(CV, note, true);
            }
            
            VC = findInOto(VC, note, true);
            VV = findInOto(VV, note, true);
            cVC = findInOto(cVC, note);
            endSoundVowel = findInOto(endSoundVowel, note);
            endSoundLastConsonant = findInOto(endSoundLastConsonant, note);

            if (CV == null){
                CV = findInOto(CV_noSuffix, note);
            }
            if (frontCV == null) {
                frontCV = CV;
            }
            if (VV == null) {
                // VV음소 없으면 (ex : a i) 대응하는 CV음소 사용 (ex:  i)
                VV = CV;
            }


                return cbnnPhonemes;
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
                Hashtable variated = hangeul.variate(prevNeighbour, note, nextNeighbour);
                thisFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[3]][1]);
                thisLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[5]][2]);
                prevFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[0]][1]);
                prevLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[2]][2]);
                nextFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[6]][1]);
                nextLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[8]][2]);
                return convertForCBNN(variated);
            }

            public Hashtable convertForCBNN(Note? prevNeighbour) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCBNNSingle(hangeul.variate(prevNeighbour?.lyric));

            }

            public bool thisHasBatchim(){
                if (!thisLastConsonant.Equals("")){
                    return true;
                }
                else{
                    return false;
                }
            }

            public bool prevHasBatchim(){
                if (!prevLastConsonant.Equals("")){
                    return true;
                }
                else{
                    return false;
                }
            }

            public bool nextHasBatchim(){
                if (!nextLastConsonant.Equals("")){
                    return true;
                }
                else{
                    return false;
                }
            }
            /// <summary>
            /// 초성이 예사소리인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsNormal(){
                if (thisFirstConsonantType == ConsonantType.NORMAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 소리가 예사소리인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsNormal(){
                if (nextFirstConsonantType == ConsonantType.NORMAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 소리가 예사소리인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsNormal(){
                if (prevFirstConsonantType == ConsonantType.NORMAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 된소리인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsFortis(){
                if (thisFirstConsonantType == ConsonantType.FORTIS){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 소리가 된소리인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsFortis(){
                if (nextFirstConsonantType == ConsonantType.FORTIS){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 소리가 된소리인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsFortis(){
                if (prevFirstConsonantType == ConsonantType.FORTIS){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 거센소리인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsAspirate(){
                if (thisFirstConsonantType == ConsonantType.ASPIRATE){
                    return true;
                }
                else{
                    return false;
                }
            }
            
            /// <summary>
            /// 다음 초성이 거센소리인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsAspirate(){
                if (nextFirstConsonantType == ConsonantType.ASPIRATE){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 초성이 거센소리인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsAspirate(){
                if (prevFirstConsonantType == ConsonantType.ASPIRATE){
                    return true;
                }
                else{
                    return false;
                }
            }
            /// <summary>
            /// 초성이 마찰음인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsFricative(){
                if (thisFirstConsonantType == ConsonantType.FRICATIVE){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 초성이 마찰음인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsFricative(){
                if (nextFirstConsonantType == ConsonantType.FRICATIVE){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 초성이 마찰음인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsFricative(){
                if (prevFirstConsonantType == ConsonantType.FRICATIVE){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 ㅇ인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsNone(){
                if (thisFirstConsonantType == ConsonantType.NOCONSONANT){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 초성이 ㅇ인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsNone(){
                if (nextFirstConsonantType == ConsonantType.NOCONSONANT){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전초성이 ㅇ인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsNone(){
                if (prevFirstConsonantType == ConsonantType.NOCONSONANT){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 비음인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsNasal(){
                if (thisFirstConsonantType == ConsonantType.NASAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 초성이 비음인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsNasal(){
                if (nextFirstConsonantType == ConsonantType.NASAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 초성이 비음인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsNasal(){
                if (prevFirstConsonantType == ConsonantType.NASAL){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 유음인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsLiquid(){
                if (thisFirstConsonantType == ConsonantType.LIQUID){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 초성이 유음인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsLiquid(){
                if (nextFirstConsonantType == ConsonantType.LIQUID){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 초성이 유음인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsLiquid(){
                if (prevFirstConsonantType == ConsonantType.LIQUID){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 ㅎ인지 판단합니다.
            /// </summary>
            public bool thisFirstConsonantIsH(){
                if (thisFirstConsonantType == ConsonantType.H){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 ㅎ인지 판단합니다.
            /// </summary>
            public bool nextFirstConsonantIsH(){
                if (thisFirstConsonantType == ConsonantType.H){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 초성이 ㅎ인지 판단합니다.
            /// </summary>
            public bool prevFirstConsonantIsH(){
                if (prevFirstConsonantType == ConsonantType.H){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 현재 글자가 VV가 적용되는 순수 모음인지 판단합니다.
            /// </summary>
            public bool thisIsPlainVowel(){
                if (thisFirstConsonantIsNone() && thisVowelHead.Equals("") ){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 글자가 VV가 적용되는 순수 모음인지 판단합니다.
            /// </summary>
            public bool nextIsPlainVowel(){
                if (nextFirstConsonantIsNone() && nextVowelHead.Equals("")){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 글자가 VV가 적용되는 순수 모음인지 판단합니다.
            /// </summary>
            public bool prevIsPlainVowel(){
                if (prevFirstConsonantIsNone() && prevVowelHead.Equals("")){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 종성이 비음인지 판단합니다.
            /// </summary>
            public bool thisLastConsonantIsNasal(){
                if (thisLastConsonantType == BatchimType.NASAL_END){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 종성이 비음인지 판단합니다.
            /// </summary>
            public bool nextLastConsonantIsNasal(){
                if (nextLastConsonantType == BatchimType.NASAL_END){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 종성이 비음인지 판단합니다.
            /// </summary>
            public bool prevLastConsonantIsNasal(){
                if (prevLastConsonantType == BatchimType.NASAL_END){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 종성이 유음인지 판단합니다.
            /// </summary>
            public bool thisLastConsonantIsLiquid(){
                if (thisLastConsonantType == BatchimType.LIQUID_END){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 다음 종성이 비음인지 판단합니다.
            /// </summary>
            public bool nextLastConsonantIsLiquid(){
                if (nextLastConsonantType == BatchimType.LIQUID_END){
                    return true;
                }
                else{
                    return false;
                }
            }

            /// <summary>
            /// 이전 종성이 유음인지 판단합니다.
            /// </summary>
            public bool prevLastConsonantIsLiquid(){
                if (prevLastConsonantType == BatchimType.LIQUID_END){
                    return true;
                }
                else{
                    return false;
                }
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
            int vcLength = 120; 
            int vcLengthShort = 90;
            Hanguel hanguel = new Hanguel();
            CBNN CBNN = new CBNN(singer, thisNote, totalDuration, vcLength, vcLengthShort);

            try{
                // change lyric to CBNN phonemes, with phoneme variation.
                cbnnPhonemes = CBNN.convertForCBNN(prevNote, thisNote, nextNote);
            }
            catch {
                return generateResult(lyric);
            }
                

            // Return phonemes
            if ((prevNeighbour == null) && (nextNeighbour == null)) { // No neighbours / 냥
                if (! CBNN.thisHasBatchim()) { // No batchim / 냐
                    return generateResult(CBNN.frontCV, CBNN.endSoundVowel, totalDuration, vcLengthShort);
                } 
                else { // batchim
                    return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength);
                }
            } 

            else if ((prevNeighbour != null) && (nextNeighbour == null)) { // Prev neighbour only / 냥[냥]
                if (! CBNN.thisHasBatchim()) { // No Batchim / 냐[냐]
                    if (CBNN.thisFirstConsonantIsNone() && (! CBNN.prevHasBatchim()) && (! CBNN.thisHasBatchim()) && CBNN.thisIsPlainVowel()) {// when comes Vowel and there's no previous batchim / 냐[아]
                        return generateResult(CBNN.VV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8); 
                    } 
                    else if (CBNN.thisFirstConsonantIsNone() && CBNN.prevHasBatchim()) {// when came Vowel behind Batchim / 냥[아]
                        return generateResult(CBNN.CV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8);
                    } 
                    else {// Not vowel / 냐[냐]
                        if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// - CV
                            return generateResult(CBNN.frontCV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8);
                        } else {// -CV 
                            return generateResult(CBNN.CV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8);
                        }
                    }
                } 
                else if (CBNN.thisLastConsonantIsNasal() || CBNN.thisLastConsonantIsLiquid()) {// Batchim - ㄴㄹㅇㅁ  / 냐[냥]
                    if ((!CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {// when comes Vowel and there's no previous batchim / 냐[앙]
                        return generateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.vcLength, 3);
                    } 
                    else if (CBNN.prevHasBatchim() && CBNN.thisIsPlainVowel()) {// when came Vowel behind Batchim / 냥[앙]
                        return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.vcLength, 3);
                    } 
                    else {// batchim / 냐[냑]
                        if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// - CV
                            return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.vcLength, 3);
                        } 
                        else {// 이외 음소는 - CV로 이음
                            return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.vcLength, 3);
                        }
                    }
                } 
                else {// 유음받침 아니고 비음받침도 아님
                    return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 3);
                }
            } 

            else if ((prevNeighbour == null) && (nextNeighbour != null)) {// next lyric is Hangeul
                if (hanguel.isHangeul(nextNeighbour?.lyric)) {// Next neighbour only  / null [아] 아
                    if (!CBNN.thisHasBatchim()) { // No batchim / null [냐] 냥
                        if (CBNN.nextIsPlainVowel() || CBNN.VC == null) {// there should be No VC phoneme for next VV phoneme
                            return generateResult(CBNN.frontCV);
                        } 
                        else{
                            return generateResult(CBNN.frontCV, CBNN.VC, totalDuration, CBNN.vcLength, 3);
                        } 
                    } 
                    else if (CBNN.thisLastConsonantIsNasal() || CBNN.thisLastConsonantIsLiquid()) {// Batchim - ㄴㄹㅇㅁ / null [냥]냐
                        if (CBNN.nextFirstConsonantIsNormal() || CBNN.nextFirstConsonantIsNasal() || CBNN.nextFirstConsonantIsLiquid() || CBNN.nextFirstConsonantIsNone()) {
                            return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                        } 
                        else {// 다음 음소가 나머지임
                            return generateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                        }
                    } 
                    else {// 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                    }
                } 
                else { // 뒤에 한글 안옴
                    if (! CBNN.thisHasBatchim()) {
                        return generateResult(CBNN.frontCV);
                    } 
                    else {
                        return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 3);
                    }
                } 
            } 

            else if ((prevNeighbour != null) && (nextNeighbour != null)) {// 둘다 이웃 있음
                if (hanguel.isHangeul(nextNeighbour?.lyric)) {// 뒤의 이웃이 한국어임
                    if (! CBNN.thisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel() && CBNN.nextFirstConsonantIsNone()) {// 앞에 받침 없는 모음 / 뒤에 모음 옴 / 냐[아]아
                            return generateResult(CBNN.VV);
                        } 
                        else if ((! CBNN.prevHasBatchim()) && (! CBNN.nextFirstConsonantIsNone()) && CBNN.thisIsPlainVowel()) {// 앞에 받침 없는 모음 / 뒤에 자음 옴 / 냐[아]냐
                            if (CBNN.VC != null) {
                                return generateResult(CBNN.VV, CBNN.VC, totalDuration, CBNN.vcLength);
                            } 
                            else {// No VC
                                return generateResult(CBNN.VV);
                            } 
                        }
                        else {// 앞에 받침 있고 뒤에 모음 옴 / 냐[냐]아  냥[아]아
                            if (CBNN.nextIsPlainVowel()) {
                                if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return generateResult(CBNN.frontCV);
                                } 
                                else {
                                    return generateResult(CBNN.CV);
                                }
                            } 
                            else {// 앞에 받침 있고 뒤에 모음 안옴
                                if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    if (CBNN.VC != null) {
                                        return generateResult(CBNN.frontCV, CBNN.VC, totalDuration, CBNN.vcLengthShort);
                                    } 
                                    else {
                                        return generateResult(CBNN.frontCV);
                                    }
                                } 
                                else {// 나머지 / CV로 시작
                                    if (CBNN.VC != null) {
                                        return generateResult(CBNN.CV, CBNN.VC, totalDuration, CBNN.vcLengthShort);
                                    } 
                                    else {
                                        return generateResult(CBNN.CV);
                                    }
                                }
                            }
                        }
                    } 
                    else if (CBNN.thisHasBatchim() && (CBNN.nextFirstConsonantIsFricative() || CBNN.thisLastConsonantIsNasal() || CBNN.thisLastConsonantIsLiquid() || CBNN.nextFirstConsonantIsNone())) {// 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ인 아무런 받침 / 냐[냥]냐
                        if (CBNN.nextFirstConsonantIsNormal() || CBNN.nextFirstConsonantIsNasal() || CBNN.nextFirstConsonantIsNone() || CBNN.nextFirstConsonantIsLiquid()) {
                            // 다음 음소가 ㄱㄷㅂㅅㅈㄴㅇㄹㅇ 임
                            if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {
                                // 앞에 받침 없고 받침 있는 모음 / 냐[앙]냐
                                return generateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                            } 
                            else {
                                // 앞에 받침 있고 받침 오는 CV / 냥[냥]냐 
                                if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함
                                    return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                                } 
                                else {
                                    return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                                }
                            }
                        } 
                        else {// 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {// 앞에 받침 없는 모음 / 냐[앙]꺅
                                return generateResult(CBNN.VV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                            } 
                            else {// 앞에 받침 있고 받침 있는 CVC / 냥[냥]꺅
                                if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return generateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                                } 
                                else {// 나머지 음소 
                                    return generateResult(CBNN.CV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                                }
                            }
                        }
                    } 
                    else {// 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {// 앞에 받침 없는 모음 / 냐[악]꺅
                            return generateResult(CBNN.VV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                        } 
                        else {// 앞에 받침이 온 CVC 음소(받침 있음) / 냥[악]꺅  냥[먁]꺅
                            if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                return generateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                            } 
                            else {// 나머지
                                return generateResult(CBNN.CV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2);
                            }
                        }
                    }
                } 
                else if ((bool)(nextNeighbour?.lyric.Equals("-")) || (bool)(nextNeighbour?.lyric.Equals("R"))) {// 둘다 이웃 있고 뒤에 -가 옴
                    if (! CBNN.thisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel() && CBNN.nextFirstConsonantIsNone()) {// 앞에 받침 없는 모음 / 냐[아]냐
                            return generateResult(CBNN.VV);
                        } 
                        else if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())){
                            return generateResult(CBNN.frontCV);
                        }
                        else {// 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            return generateResult(CBNN.CV);
                        }
                    } 
                    else {
                        if (CBNN.nextFirstConsonantIsLiquid() || CBNN.nextFirstConsonantIsNasal() || CBNN.nextFirstConsonantIsNone()) {// 다음 음소가 ㄴㅇㄹㅇ 임
                            if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {// 앞에 받침 없는 모음 / 냐[악]꺅
                            return generateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                        } 
                        else {// 앞에 받침이 온 CVC 음소(받침 있음) / 냥[악]꺅  냥[먁]꺅
                            if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                            } 
                            else {// 나머지
                                return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                            }
                        }
                    } 
                        else {// 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((! CBNN.prevHasBatchim()) && CBNN.thisIsPlainVowel()) {// 앞에 받침 없는 모음 / 냐[앙]꺅
                                return generateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                            } 
                            else {// 앞에 받침 있고 받침 있는 CVC / 냥[냥]꺅
                                if (CBNN.prevHasBatchim() && (CBNN.thisFirstConsonantIsAspirate() || CBNN.thisFirstConsonantIsFortis() || CBNN.thisFirstConsonantIsFricative())) {// ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return generateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                                } 
                                else {// 나머지 음소 
                                    return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                                }
                            }
                        }
                    } 
                } 
                else {
                    if (! CBNN.thisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]-
                        return generateResult(CBNN.CV);
                    } 
                    else {// 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]-
                        return generateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 3);
                    }
                }
            } 
            else {
                return generateResult(CBNN.CV);
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
            CBNN CBNN = new CBNN(singer, thisNote, totalDuration, vcLength, vcLengthShort);
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
                return generateResult(phonemeToReturn);
            } else if (thisNote.lyric.Equals("R")) {
                if (hanguel.isHangeul(prevLyric)) {
                    cbnnPhonemes = CBNN.convertForCBNN(prevNote);

                    string prevVowelTail = (string)cbnnPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cbnnPhonemes[4]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} R";
                    } 
                    else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} R";
                    }

                }
                return generateResult(phonemeToReturn);
            } 
            else {
                return generateResult(phonemeToReturn);
            }
        }
    }
}