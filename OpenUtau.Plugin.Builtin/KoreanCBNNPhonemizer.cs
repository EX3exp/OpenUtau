using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using Melanchall.DryWetMidi.Core;
using NumSharp.Extensions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;
using SharpCompress;

namespace OpenUtau.Plugin.Builtin {
     /// Phonemizer for 'KOR CBNN(Combination)' ///
    [Phonemizer("Korean CBNN Phonemizer", "KO CBNN", "EX3", language:"KO")]

    public class KoreanCBNNPhonemizer : Phonemizer {

        // 1. Load Singer
        private USinger singer;
		public override void SetSinger(USinger singer) => this.singer = singer;
        //

        // 
        public class Hanguel{
            const string FIRST_CONSONANTS = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
            const string MIDDLE_VOWELS = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
            const string LAST_CONSONANTS = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ"; // The first blank(" ") is needed because Hangeul may not have lastConsonant.
            
            const ushort HANGEUL_UNICODE_START = 0xAC00; // unicode index of 가
            const ushort HANGEUL_UNICODE_END = 0xD79F; // unicode index of 힣
            
            static readonly Hashtable basicSounds = new Hashtable() {
                    ["ㄱ"] = 0,
                    ["ㄷ"] = 1,
                    ["ㅂ"] = 2,
                    ["ㅈ"] = 3,
                    ["ㅅ"] = 4
                };

            static readonly Hashtable aspirateSounds = new Hashtable() {
                    [0] = "ㅋ",
                    [1] = "ㅌ",
                    [2] = "ㅍ",
                    [3] = "ㅊ",
                    [4] = "ㅌ"
                };

            static readonly Hashtable fortisSounds = new Hashtable() {
                    [0] = "ㄲ",
                    [1] = "ㄸ",
                    [2] = "ㅃ",
                    [3] = "ㅉ",
                    [4] = "ㅆ"
                };

            static readonly Hashtable nasalSounds = new Hashtable() {
                    ["ㄴ"] = 0,
                    ["ㅇ"] = 1,
                    ["ㅁ"] = 2
                };

            public Hanguel(){}

            public bool isHangeul(string? character){
                /// <summary>
                /// true when input character is hangeul.
                /// </summary>
                ushort unicodeIndex;
                bool isHangeul;

                if (character != null) {
                    unicodeIndex = Convert.ToUInt16(character[0]);
                    isHangeul = !(unicodeIndex < HANGEUL_UNICODE_START || unicodeIndex > HANGEUL_UNICODE_END); 
                }
                else {
                    isHangeul = false;
                }

                return isHangeul;
            }

            public Hashtable separate(string character){
            /// <summary>
            /// Separates complete hangeul character in three parts - firstConsonant(초성), middleVowel(중성), lastConsonant(종성).
            /// </summary>
            /// <param name="hangeul">A complete Hangeul character.
            /// (ex) '냥' 
            /// </param>
            /// <returns>{firstConsonant(초성), middleVowel(중성), lastConsonant(종성)}
            /// (ex) {"ㄴ", "ㅑ", "ㅇ"}
            /// </returns>
                int hangeulIndex; // unicode index of hangeul - unicode index of '가' (ex) '냥'

                int firstConsonantIndex; // (ex) 2
                int middleVowelIndex; // (ex) 2
                int lastConsonantIndex; // (ex) 21

                string firstConsonant; // (ex) "ㄴ"
                string middleVowel; // (ex) "ㅑ"
                string lastConsonant; // (ex) "ㅇ"

                Hashtable separatedHangeul; // (ex) {[0]: "ㄴ", [1]: "ㅑ", [2]: "ㅇ"}


                hangeulIndex = Convert.ToUInt16(character[0]) - HANGEUL_UNICODE_START; 


                lastConsonantIndex = hangeulIndex % 28; // seperates lastConsonant
                hangeulIndex = (hangeulIndex - lastConsonantIndex) / 28;

                middleVowelIndex = hangeulIndex % 21; // seperates middleVowel
                hangeulIndex = (hangeulIndex - middleVowelIndex) / 21;

                firstConsonantIndex = hangeulIndex; // there's only firstConsonant now

                firstConsonant = FIRST_CONSONANTS[firstConsonantIndex].ToString();
                middleVowel = MIDDLE_VOWELS[middleVowelIndex].ToString();
                lastConsonant = LAST_CONSONANTS[lastConsonantIndex].ToString();

                separatedHangeul = new Hashtable() {
                [0] = firstConsonant,
                [1] = middleVowel,
                [2] = lastConsonant
                };

                
                return separatedHangeul;
            }

            
            private Hashtable variate(Hashtable firstCharSeparated, Hashtable nextCharSeparated, int returnCharIndex=-1){
                /// 두 글자에서 음운변동 적용 
                /// 분리된 걸 해시테이블로 넣어줘야 함
                /// 맨 끝 노트가 아닌 곳에서만 씀!!! 맨 끝 노트에는 variate(string character) 사용
                /// 갈아 라고 넣으면 가라 라고 나옴
                /// 앉아 는 안자 됨
                /// 맑다 는 막따 됨
                /// returnChar = -1 : 글자 둘 다 반환
                /// returnChar = 0 : 첫 번째 글자 반환
                /// returnChar = 1 : 두 번째 글자 반환
                /// returnChar 나머지 : 글자 둘 다 반환
                
                string firstLastConsonant = (string)firstCharSeparated[2]; // 문래 에서 ㄴ, 맑다 에서 ㄺ
                string nextFirstConsonant = (string)nextCharSeparated[0]; // 문래 에서 ㄹ, 맑다 에서 ㄷ

                // 1. 연음 적용
                if ((nextFirstConsonant.Equals("ㅇ")) && (!firstLastConsonant.Equals(" "))){
                    // ㄳ ㄵ ㄶ ㄺ ㄻ ㄼ ㄽ ㄾ ㄿ ㅀ ㅄ 일 경우에도 분기해서 연음 적용
                    if (firstLastConsonant.Equals("ㄳ")) {
                        firstLastConsonant = "ㄱ";
                        nextFirstConsonant = "ㅅ";
                        }
                    else if (firstLastConsonant.Equals("ㄵ")) {
                        firstLastConsonant = "ㄴ";
                        nextFirstConsonant = "ㅈ";
                    }
                    else if (firstLastConsonant.Equals("ㄶ")) {
                        firstLastConsonant = "ㄴ";
                        nextFirstConsonant = "ㅎ";
                    }
                    else if (firstLastConsonant.Equals("ㄺ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㄱ";
                    }
                    else if (firstLastConsonant.Equals("ㄼ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㅂ";
                    }
                    else if (firstLastConsonant.Equals("ㄽ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㅅ";
                    }
                    else if (firstLastConsonant.Equals("ㄾ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㅌ";
                    }
                    else if (firstLastConsonant.Equals("ㄿ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㅍ";
                    }
                    else if (firstLastConsonant.Equals("ㅀ")) {
                        firstLastConsonant = "ㄹ";
                        nextFirstConsonant = "ㅎ";
                    }
                    else if (firstLastConsonant.Equals("ㅄ")) {
                        firstLastConsonant = "ㅂ";
                        nextFirstConsonant = "ㅅ";
                    }
                    else {
                        // 겹받침 아닐 때 연음
                        nextFirstConsonant = firstLastConsonant;
                        firstLastConsonant = " ";
                    }
                }


                // 1. 유기음화 및 ㅎ탈락 1
                if ((firstLastConsonant.Equals("ㅎ")) && (! nextFirstConsonant.Equals("ㅅ")) && (basicSounds.Contains(nextFirstConsonant))){
                    // ㅎ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 낳다 = 나타
                    firstLastConsonant = " ";
                    nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
                }
                else if ((firstLastConsonant.Equals("ㅎ")) && (! nextFirstConsonant.Equals("ㅅ")) && (nextFirstConsonant.Equals("ㅇ"))){
                    // ㅎ으로 끝나고 다음 소리가 없으면 / ex) 낳아 = 나아
                    firstLastConsonant = " ";
                }
                
                else if ((firstLastConsonant.Equals("ㄶ")) && (! nextFirstConsonant.Equals("ㅅ")) && (basicSounds.Contains(nextFirstConsonant))){
                    // ㄶ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 많다 = 만타
                    firstLastConsonant = "ㄴ";
                    nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
                }
                else if ((firstLastConsonant.Equals("ㅀ")) && (! nextFirstConsonant.Equals("ㅅ")) && (basicSounds.Contains(nextFirstConsonant))){
                    // ㅀ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 끓다 = 끌타
                    firstLastConsonant = "ㄹ";
                    nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
                }
                

                
                // 2-1. 된소리되기 1
                if (((firstLastConsonant.Equals("ㄳ")) || (firstLastConsonant.Equals("ㄵ")) || (firstLastConsonant.Equals("ㄽ")) || (firstLastConsonant.Equals("ㄾ")) || (firstLastConsonant.Equals("ㅄ")) || (firstLastConsonant.Equals("ㄼ")) || (firstLastConsonant.Equals("ㄺ")) || (firstLastConsonant.Equals("ㄿ"))) && (basicSounds.Contains(nextFirstConsonant))){
                    // [ㄻ, (ㄶ, ㅀ)<= 유기음화에 따라 예외] 제외한 겹받침으로 끝나고 다음 소리가 예사소리이면
                    nextFirstConsonant = (string)fortisSounds[basicSounds[nextFirstConsonant]];
                }
                
                // 3. 첫 번째 글자의 자음군단순화 및 평파열음화(음절의 끝소리 규칙)
                if ((firstLastConsonant.Equals("ㄽ")) || (firstLastConsonant.Equals("ㄾ")) || (firstLastConsonant.Equals("ㄼ"))){
                    firstLastConsonant = "ㄹ";
                }
                else if ((firstLastConsonant.Equals("ㄵ")) || (firstLastConsonant.Equals("ㅅ")) || (firstLastConsonant.Equals("ㅆ")) || (firstLastConsonant.Equals("ㅈ")) || (firstLastConsonant.Equals("ㅉ")) || (firstLastConsonant.Equals("ㅊ"))){
                    firstLastConsonant = "ㄷ";
                }
                else if ((firstLastConsonant.Equals("ㅃ")) || (firstLastConsonant.Equals("ㅍ")) || (firstLastConsonant.Equals("ㄿ")) || (firstLastConsonant.Equals("ㅄ"))){
                    firstLastConsonant = "ㅂ";
                }
                else if ((firstLastConsonant.Equals("ㄲ")) || (firstLastConsonant.Equals("ㅋ")) || (firstLastConsonant.Equals("ㄺ")) || (firstLastConsonant.Equals("ㄳ"))){
                    firstLastConsonant = "ㄱ";
                }
                else if ((firstLastConsonant.Equals("ㄻ"))){
                    firstLastConsonant = "ㅁ";
                }

                // 2-1. 된소리되기 2
                if ((basicSounds.Contains(firstLastConsonant)) && (basicSounds.Contains(nextFirstConsonant))){
                    // 예사소리로 끝나고 다음 소리가 예사소리이면 / ex) 닭장 = 닥짱
                    nextFirstConsonant = (string)fortisSounds[basicSounds[nextFirstConsonant]];
                }

                // 1. 유기음화 2
                if ((basicSounds.Contains(firstLastConsonant)) && (nextFirstConsonant.Equals("ㅎ"))){
                    // ㄱㄷㅂㅈ(+ㅅ)로 끝나고 다음 소리가 ㅎ이면 / ex) 축하 = 추카, 옷하고 = 오타고
                    // ㅅ은 미리 평파열음화가 진행된 것으로 보고 ㄷ으로 간주한다
                    nextFirstConsonant = (string)aspirateSounds[basicSounds[firstLastConsonant]];
                    firstLastConsonant = " ";
                }
                else if (nextFirstConsonant.Equals("ㅎ")){
                    nextFirstConsonant = "ㅇ";
                }


               // 4. 비음화
                if ((firstLastConsonant.Equals("ㄱ")) && (! nextFirstConsonant.Equals("ㅇ")) && ((nasalSounds.Contains(nextFirstConsonant)) || (nextFirstConsonant.Equals("ㄹ")))){
                    // ex) 막론 = 망론 >> 망논 
                    firstLastConsonant = "ㅇ";
               }
                else if ((firstLastConsonant.Equals("ㄷ")) && (! nextFirstConsonant.Equals("ㅇ")) && ((nasalSounds.Contains(nextFirstConsonant)) || (nextFirstConsonant.Equals("ㄹ")))){
                    // ex) 슬롯머신 = 슬론머신
                    firstLastConsonant = "ㄴ";
               }
                else if ((firstLastConsonant.Equals("ㅂ")) && (! nextFirstConsonant.Equals("ㅇ")) && ((nasalSounds.Contains(nextFirstConsonant)) || (nextFirstConsonant.Equals("ㄹ")))){
                    // ex) 밥먹자 = 밤먹자 >> 밤먹짜
                    firstLastConsonant = "ㅁ";
               }

                // 4'. 유음화
                if ((firstLastConsonant.Equals("ㄴ")) && nextFirstConsonant.Equals("ㄹ")){
                    // ex) 만리 = 말리
                    firstLastConsonant = "ㄹ";
                }
                else if ((firstLastConsonant.Equals("ㄹ")) && nextFirstConsonant.Equals("ㄴ")){
                    // ex) 칼날 = 칼랄
                    nextFirstConsonant = "ㄹ";
                }

                // 4''. ㄹ비음화
                if ((nextFirstConsonant.Equals("ㄹ")) && (nasalSounds.Contains(nextFirstConsonant))){
                    // ex) 담력 = 담녁
                    firstLastConsonant = "ㄴ";
                }
                
                
                // 4'''. 자음동화
                if ((firstLastConsonant.Equals("ㄴ")) && nextFirstConsonant.Equals("ㄱ")){
                    // ex) ~라는 감정 = ~라능 감정
                    firstLastConsonant = "ㅇ";
                }
                
                // return results
                if (returnCharIndex == 0){
                    // 첫 번째 글자 반환
                    return new Hashtable(){
                        [0] = firstCharSeparated[0], 
                        [1] = firstCharSeparated[1], 
                        [2] = firstLastConsonant
                        };
                }
                else if (returnCharIndex == 1){
                    // 두 번째 글자 반환
                    return new Hashtable(){
                        [0] = nextFirstConsonant, 
                        [1] = nextCharSeparated[1], 
                        [2] = nextCharSeparated[2]
                        };
                }
                else {
                    // 두 글자 다 반환
                    return new Hashtable(){
                        [0] = firstCharSeparated[0], 
                        [1] = firstCharSeparated[1], 
                        [2] = firstLastConsonant, 
                        [3] = nextFirstConsonant, 
                        [4] = nextCharSeparated[1], 
                        [5] = nextCharSeparated[2]};
                }
            }

            private Hashtable variate(string character){
                /// 맨 끝 노트에서 음운변동 적용하는 함수
                /// 자음군 단순화와 평파열음화
                Hashtable separated = separate(character);

                if ((separated[2].Equals("ㄽ")) || (separated[2].Equals("ㄾ")) || (separated[2].Equals("ㄼ")) || (separated[2].Equals("ㅀ"))){
                    separated[2] = "ㄹ";
                }
                else if ((separated[2].Equals("ㄵ")) || (separated[2].Equals("ㅅ")) || (separated[2].Equals("ㅆ")) || (separated[2].Equals("ㅈ")) || (separated[2].Equals("ㅉ")) || (separated[2].Equals("ㅊ"))){
                    separated[2] = "ㄷ";
                }
                else if ((separated[2].Equals("ㅃ")) || (separated[2].Equals("ㅍ")) || (separated[2].Equals("ㄿ")) || (separated[2].Equals("ㅄ"))){
                    separated[2] = "ㅂ";
                }
                else if ((separated[2].Equals("ㄲ")) || (separated[2].Equals("ㅋ")) || (separated[2].Equals("ㄺ")) || (separated[2].Equals("ㄳ"))){
                    separated[2] = "ㄱ";
                }
                else if ((separated[2].Equals("ㄻ"))){
                    separated[2] = "ㅁ";
                }
                else if ((separated[2].Equals("ㄶ"))){
                    separated[2] = "ㄴ";
                }


                return separated;

            }

            private Hashtable variate(Hashtable separated){
                /// 맨 끝 노트에서 음운변동 적용하는 함수
                
                if ((separated[2].Equals("ㄽ")) || (separated[2].Equals("ㄾ")) || (separated[2].Equals("ㄼ")) || (separated[2].Equals("ㅀ"))){
                    separated[2] = "ㄹ";
                }
                else if ((separated[2].Equals("ㄵ")) || (separated[2].Equals("ㅅ")) || (separated[2].Equals("ㅆ")) || (separated[2].Equals("ㅈ")) || (separated[2].Equals("ㅉ")) || (separated[2].Equals("ㅊ"))){
                    separated[2] = "ㄷ";
                }
                else if ((separated[2].Equals("ㅃ")) || (separated[2].Equals("ㅍ")) || (separated[2].Equals("ㄿ")) || (separated[2].Equals("ㅄ"))){
                    separated[2] = "ㅂ";
                }
                else if ((separated[2].Equals("ㄲ")) || (separated[2].Equals("ㅋ")) || (separated[2].Equals("ㄺ")) || (separated[2].Equals("ㄳ"))){
                    separated[2] = "ㄱ";
                }
                else if ((separated[2].Equals("ㄻ"))){
                    separated[2] = "ㅁ";
                }
                else if ((separated[2].Equals("ㄶ"))){
                    separated[2] = "ㄴ";
                }


                return separated;

            }
            private Hashtable variate(string firstChar, string nextChar, int returnCharIndex=0){
                // 글자 넣어도 쓸 수 있음
                Hashtable firstCharSeparated = separate(firstChar);
                Hashtable nextCharSeparated = separate(nextChar);
                return variate(firstCharSeparated, nextCharSeparated, returnCharIndex);
            }

            public Hashtable variate(Note? prevNeighbour, Note note, Note? nextNeighbour){
                // prevNeighbour와 note와 nextNeighbour의 음원변동된 가사를 반환
                // prevNeighbour : VV 정렬에 사용
                // nextNeighbour : VC 정렬에 사용
                // 뒤의 노트가 없으면 리턴되는 값의 6~8번 인덱스가 null로 채워진다.
                int whereYeonEum = -1;
                // -1 : 해당사항 없음
                // 0 : 이전 노트를 연음하지 않음
                // 1 : 현재 노트를 연음하지 않음
                string?[] lyrics = new string?[]{prevNeighbour?.lyric, note.lyric, nextNeighbour?.lyric};
                

                if (! isHangeul(lyrics[0])){
                    // 앞노트 한국어 아니거나 null일 경우 null처리
                    if (lyrics[0] != null){
                        lyrics[0] = null;
                    }
                }
                else if (! isHangeul(lyrics[2])){
                    // 뒤노트 한국어 아니거나 null일 경우 null처리
                    if (lyrics[2] != null){
                        lyrics[2] = null;
                    }
                    
                }

                if ((lyrics[0] != null) && (lyrics[0].EndsWith('.'))){
                    /// 앞노트 . 기호로 끝남 ex) [냥.]냥냥
                    lyrics[0] = lyrics[0].TrimEnd('.');
                    whereYeonEum = 0;
                }
                else if ((lyrics[1] != null) && (lyrics[1].EndsWith('.'))){
                    /// 중간노트 . 기호로 끝남 ex) 냥[냥.]냥
                    /// 음운변동 없이 연음만 적용
                    lyrics[1] = lyrics[1].TrimEnd('.');
                    whereYeonEum = 1;
                }
                else if ((lyrics[2] != null) && (lyrics[2].EndsWith('.'))){
                    /// 뒤노트 . 기호로 끝남 ex) 냥냥[냥.]
                    /// 중간노트의 발음에 관여하지 않으므로 간단히 . 만 지워주면 된다
                    lyrics[2] = lyrics[2].TrimEnd('.');
                }

                // 음운변동 적용 --
                if ((lyrics[0] == null) && (lyrics[2] != null)){
                    /// 앞이 없고 뒤가 있음
                    /// null[냥]냥
                    if (whereYeonEum == 1){
                        // 현재 노트에서 단어가 끝났다고 가정
                        Hashtable result = variate(separate(lyrics[0]), variate(lyrics[1]), 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(separate(lyrics[0]), variate(lyrics[1]), 1), separate(lyrics[2]), -1); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, thisNoteSeparated[3]); // 뒤 글자 없음
                        result.Add(7, thisNoteSeparated[4]);
                        result.Add(8, thisNoteSeparated[5]);

                        return result;
                    }
                    else if (whereYeonEum == 0){
                        // 앞 노트에서 단어가 끝났다고 가정 
                        Hashtable result = variate(variate(lyrics[0]), separate(lyrics[1]), 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(variate(lyrics[0]), separate(lyrics[1]), 1), separate(lyrics[2]), -1); // 첫 글자와 현 글자 / 앞글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, thisNoteSeparated[3]); // 뒤 글자 없음
                        result.Add(7, thisNoteSeparated[4]);
                        result.Add(8, thisNoteSeparated[5]);

                        return result;
                    }
                    else{
                        Hashtable result = new Hashtable() {
                        [0] = "null", // 앞 글자 없음
                        [1] = "null",
                        [2] = "null"
                    };

                    Hashtable thisNoteSeparated = variate(lyrics[1], lyrics[2], -1); // 현글자 뒤글자
                    
                    result.Add(3, thisNoteSeparated[0]); // 현 글자
                    result.Add(4, thisNoteSeparated[1]);
                    result.Add(5, thisNoteSeparated[2]);

                    result.Add(6, thisNoteSeparated[3]); // 뒤 글자 없음
                    result.Add(7, thisNoteSeparated[4]);
                    result.Add(8, thisNoteSeparated[5]);
                    
                    return result;
                    }
                }
                else if ((lyrics[0] != null) && (lyrics[2] == null)){
                    /// 앞이 있고 뒤는 없음
                    /// 냥[냥]null
                    if (whereYeonEum == 1){
                        // 현재 노트에서 단어가 끝났다고 가정
                        Hashtable result = variate(separate(lyrics[0]), variate(lyrics[1]), 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(separate(lyrics[0]), variate(lyrics[1]), 1)); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, "null"); // 뒤 글자 없음
                        result.Add(7, "null");
                        result.Add(8, "null");

                        return result;
                    }
                    else if (whereYeonEum == 0){
                        // 앞 노트에서 단어가 끝났다고 가정 
                        Hashtable result = variate(variate(lyrics[0]), separate(lyrics[1]), 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(variate(lyrics[0]), separate(lyrics[1]), 1)); // 첫 글자와 현 글자 / 앞글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, "null"); // 뒤 글자 없음
                        result.Add(7, "null");
                        result.Add(8, "null");

                        return result;
                    }
                    else{
                        Hashtable result = variate(lyrics[0], lyrics[1], 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(lyrics[0], lyrics[1], 1)); // 첫 글자와 현 글자 / 뒷글자 없으니까 글자 혼자 있는걸로 음운변동 한 번 더 시키기
                        
                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, "null"); // 뒤 글자 없음
                        result.Add(7, "null");
                        result.Add(8, "null");

                        return result;
                    }
                    
                }
                else if ((lyrics[0] != null) && (lyrics[2] != null)){
                    /// 앞도 있고 뒤도 있음
                    /// 냥[냥]냥
                    if (whereYeonEum == 1){
                        // 현재 노트에서 단어가 끝났다고 가정 / 무 [릎.] 위
                        Hashtable result = variate(separate(lyrics[0]), variate(lyrics[1]), 1); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(separate(lyrics[0]), variate(lyrics[1]), 1), separate(lyrics[2]), -1);// 현글자와 다음 글자 / 현 글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기
                        
                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, thisNoteSeparated[3]); // 뒤 글자
                        result.Add(7, thisNoteSeparated[4]);
                        result.Add(8, thisNoteSeparated[5]);

                        return result; 
                    }
                    else if (whereYeonEum == 0){
                        // 앞 노트에서 단어가 끝났다고 가정 / 릎. [위] 놓
                        Hashtable result = variate(variate(lyrics[0]), separate(lyrics[1]), 0); // 첫 글자
                        Hashtable thisNoteSeparated = variate(variate(variate(lyrics[0]), separate(lyrics[1]), 1), separate(lyrics[2]), -1); // 현 글자와 뒤 글자 / 앞글자 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기
                        
                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, thisNoteSeparated[3]); // 뒤 글자
                        result.Add(7, thisNoteSeparated[4]);
                        result.Add(8, thisNoteSeparated[5]);

                        return result;
                    }
                    else{
                        Hashtable result = variate(lyrics[0], lyrics[1], 0);
                        Hashtable thisNoteSeparated = variate(variate(lyrics[0], lyrics[1], 1), separate(lyrics[2]), -1);

                        result.Add(3, thisNoteSeparated[0]); // 현 글자
                        result.Add(4, thisNoteSeparated[1]);
                        result.Add(5, thisNoteSeparated[2]);

                        result.Add(6, thisNoteSeparated[3]); // 뒤 글자
                        result.Add(7, thisNoteSeparated[4]);
                        result.Add(8, thisNoteSeparated[5]);

                        return result;
                    }
                }
                else {
                    /// 앞이 없고 뒤도 없음
                    /// null[냥]null
                    
                    Hashtable result = new Hashtable(){
                        // 첫 글자 >> 비어 있음
                        [0] = "null",
                        [1] = "null",
                        [2] = "null"
                    };

                    Hashtable thisNoteSeparated = variate(lyrics[1]); // 현 글자

                    result.Add(3, thisNoteSeparated[0]); // 현 글자
                    result.Add(4, thisNoteSeparated[1]);
                    result.Add(5, thisNoteSeparated[2]);

                        
                    result.Add(6, "null"); // 뒤 글자 비어있음
                    result.Add(7, "null");
                    result.Add(8 , "null");

                    return result;
                }
                
            }
            
        }
        
        private class CBNN{
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

            public CBNN(){}
            public Hashtable convertForCBNN(Hashtable separated){
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // VV 음소를 위해 앞의 노트의 변동된 결과까지 반환한다
                // vc 음소를 위해 뒤의 노트의 변동된 결과까지 반환한다
                Hashtable separatedConvertedForCBNN;

                separatedConvertedForCBNN = new Hashtable(){
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                    [5] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [7] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [8] = LAST_CONSONANTS[(string)separated[5]][1],
                    [9] = LAST_CONSONANTS[(string)separated[5]][0],

                    [10] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [11] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [12] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [13] = LAST_CONSONANTS[(string)separated[8]][1],
                    [14] = LAST_CONSONANTS[(string)separated[8]][0]
                    };

                return separatedConvertedForCBNN;
            }

            public Hashtable convertForCBNN(string character){
                // 글자를 넣으면 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                Hashtable separatedConvertedForCBNN;

                Hashtable separated = hanguel.separate(character);

                separatedConvertedForCBNN = new Hashtable(){
                    // 앞의 노트
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                    // 현재 노트
                    [5] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [7] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [8] = LAST_CONSONANTS[(string)separated[5]][1],
                    [9] = LAST_CONSONANTS[(string)separated[5]][0],

                    // 뒤의 노트
                    [10] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [11] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [12] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [13] = LAST_CONSONANTS[(string)separated[8]][1],
                    [14] = LAST_CONSONANTS[(string)separated[8]][0]
                };

                return separatedConvertedForCBNN;
            }

            public Hashtable convertForCBNN(Note? prevNeighbour, Note note, Note? nextNeighbour){
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCBNN(hanguel.variate(prevNeighbour, note, nextNeighbour));
                
            }
            
        }
        

        


        // 2. Return Phonemes
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours){
            Hashtable cbnnPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration); 
            int vcLength = 120; // TODO
            int vcLengthShort = 90;
            Hanguel hanguel = new Hanguel();
            CBNN CBNN = new CBNN();
            ///


            if (hanguel.isHangeul(lyric)){
                cbnnPhonemes = CBNN.convertForCBNN(prevNote, thisNote, nextNote); 
                // 음운변동이 진행됨 => 위에서 반환된 음소로 전부 때울 예정

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
                string cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 


                int cVCLength; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용

                if (thisLastConsonant.Equals("l")){
                    // ㄹ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("n")){
                    // ㄴ받침
                    cVCLength = 210;
                }
                else if (thisLastConsonant.Equals("ng")){
                    // ㅇ받침
                    cVCLength = 230;
                }
                else if (thisLastConsonant.Equals("m")){
                    // ㅁ받침
                    cVCLength = 280;
                }
                else if (thisLastConsonant.Equals("k")){
                    // ㄱ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("t")){
                    // ㄷ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("p")){
                    cVCLength = totalDuration / 2;
                }
                else{
                    // 나머지
                    cVCLength = totalDuration / 3;
                }

                if (thisVowelTail.Equals("u")){
                    cVCLength += 50; // 모음이 u일때엔 cVC의 발음 길이가 더 길어짐
                    vcLength += 50;
                }

                
                if ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("eu"))) {
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 eu bw 대신 eu b를 만들기 위함
                }
                else if ((thisVowelTail.Equals("y") && (thisVowelTail.Equals("i")))){
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 i by 대신 i b를 만들기 위함
                }
                else {
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}{(string)cbnnPhonemes[11]}"; // 나머지... ex) ny
                }

                string VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VV, CVC에게는 해당 없음

                
                if ((prevNeighbour == null) && (nextNeighbour == null)){
                    // 이웃이 없음 / 냥
                    
                    if (thisLastConsonant.Equals("")){ // 이웃 없고 받침 없음 / 냐
                    
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 이웃 없고 받침 있음 - ㄴㄹㅇㅁ / 냥
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                    }
                    else{
                        // 이웃 없고 받침 있음 - 나머지 / 냑
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                }

                else if ((prevNeighbour != null) && (nextNeighbour == null)){
                        // 앞에 이웃 있고 뒤에 이웃 없음 / 냥[냥]
                        if (thisLastConsonant.Equals("")){ // 뒤이웃만 없고 받침 없음 / 냐[냐]
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[아]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                        }
                        else if ((prevSuffix.Equals("")) && (! prevLastConsonant.Equals("")) && (thisSuffix.Equals("")) && (thisFirstConsonant.Equals("")) && (! thisVowelTail.Equals(""))){
                            // 앞에 받침이 온 모음 / 냥[아]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)}
                            }
                        };
                        }
                        else{
                            // 모음아님 / 냐[냐]
                            if ((thisFirstConsonant.Equals("g")) || (thisFirstConsonant.Equals("d")) || (thisFirstConsonant.Equals("b")) || (thisFirstConsonant.Equals("s")) || (thisFirstConsonant.Equals("j")) || (thisFirstConsonant.Equals("n")) || (thisFirstConsonant.Equals("r")) || (thisFirstConsonant.Equals("m")) || (thisFirstConsonant.Equals(""))){
                                // ㄱㄷㅂㅅㅈㄴㄹㅁ일 경우 CV로 이음
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                            }
                            else {
                                // 이외 음소는 - CV로 이음
                                return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                            }
                            
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))){
                        // 뒤이웃만 없고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / 냐[냥]

                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 8},
                            }
                        };
                        }
                        else if ((prevSuffix.Equals("")) && (! prevLastConsonant.Equals("")) && (thisSuffix.Equals("")) && (thisFirstConsonant.Equals("")) && (! thisVowelTail.Equals(""))){
                            // 앞에 받침이 온 모음 / 냥[앙]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            new Phoneme { phoneme = $"{thisVowelTail} -",
                            position = totalDuration - totalDuration / 8},
                            }
                        };
                        }
                        
                        else{
                            // 뒤이웃만 없고 받침 있음 - 나머지 / 냐[냑]
                            if ((thisFirstConsonant.Equals("g")) || (thisFirstConsonant.Equals("d")) || (thisFirstConsonant.Equals("b")) || (thisFirstConsonant.Equals("s")) || (thisFirstConsonant.Equals("j")) || (thisFirstConsonant.Equals("n")) || (thisFirstConsonant.Equals("r")) || (thisFirstConsonant.Equals("m")) || (thisFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("s"))){
                                // 앞받침 있고 다음이 ㄱㄷㅂㅅㅈㄴㄹㅁ일 경우 CV로 이음
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }
                        };
                            }
                            else {
                                // 이외 음소는 - CV로 이음
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 8},
                            }
                        };
                            }
                        }
                        
                    }
                    else{
                        
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - totalDuration / 2},
                            }
                        };
                    }

                        
                    }
                else if ((prevNeighbour == null) && (nextNeighbour != null)){
                    if (hanguel.isHangeul(nextNeighbour?.lyric)){
                        // 뒤 글자가 한글임
                        // 앞에 이웃 없고 뒤에 있음

                    if (thisLastConsonant.Equals("")){ // 앞이웃만 없고 받침 없음 / [냐]냥
                        if (nextFirstConsonant.Equals("")){
                            // 뒤에 VV 와야해서 VC 오면 안됨
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            }
                        };
                        }
                        
                        else{
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 앞이웃만 없고 받침 있음 - ㄴㄹㅇㅁ / [냥]냐
                        if ((nextFirstConsonant.StartsWith("g")) || (nextFirstConsonant.StartsWith("d")) || (nextFirstConsonant.StartsWith("b")) || (nextFirstConsonant.StartsWith("s")) || (nextFirstConsonant.StartsWith("j")) || (nextFirstConsonant.StartsWith("n")) || (nextFirstConsonant.StartsWith("r")) || (nextFirstConsonant.StartsWith("m")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.StartsWith("s"))){
                            return new Result(){
                        phonemes = new Phoneme[] {
                            // 다음 음소가 ㄴㅇㄹㅁㄱㄷㅂ 임 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }// -음소 없이 이어줌
                        };
                        }
                        else{
                            // 다음 음소가 나머지임
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 2},
                            }// -음소 있이 이어줌
                        };
                        }
                        
                    }
                    else{
                        // 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }
                        };
                    }
                    }
                    else if (nextNeighbour?.lyric == "-"){
                        if (thisLastConsonant.Equals("")){
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            }
                        };
                        }
                        else{
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                    }
                    else{
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                    }
                }
                else if ((prevNeighbour != null) && (nextNeighbour != null)){
                    // 둘다 이웃 있음
                    if (hanguel.isHangeul(nextNeighbour?.lyric)){
                        // 뒤의 이웃이 한국어임

                    if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals("")) && (nextFirstConsonant.Equals(""))){
                            // 앞에 받침 없는 모음 / 뒤에 모음 옴 / 냐[아]아
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            }
                        };
                        }
                        else if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 뒤에 자음 옴 / 냐[아]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        else {
                            // 앞에 받침 있고 뒤에 모음 옴 / 냐[냐]냐  냥[아]냐
                            if (nextFirstConsonant.Equals("")){
                                if ((! prevLastConsonant.Equals("")) && ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p")))){
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            }
                        };
                                }
                                else {
                                    // 나머지 / CV로 시작
                                    return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                                }
                            }
    
                            else {
                                // 앞에 받침 있고 뒤에 모음 안옴
                                if ((! prevLastConsonant.Equals("")) && ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p")))){
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                                }
                                else {
                                    // 나머지 / CV로 시작
                                    return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{VC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                                }
                                
                                
                            }

        
                            
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))){
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ인 아무런 받침 / 냐[냥]냐
                        if ((nextFirstConsonant.StartsWith("n")) || (nextFirstConsonant.StartsWith("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.StartsWith("m")) || (nextFirstConsonant.StartsWith("g")) || (nextFirstConsonant.StartsWith("d")) || (nextFirstConsonant.StartsWith("b")) || (nextFirstConsonant.StartsWith("s")) || (nextFirstConsonant.StartsWith("j"))){
                            // 다음 음소가 ㄴㅇㄹㅇ 임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없고 받침 있는 모음 / 냐[앙]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }
                        };
                        }
                        else{
                            // 앞에 받침 있고 받침 오는 CV / 냥[냥]냐 
                            if ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))){
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함
                                    return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }
                        }; 
                            }
                            else {
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }// -음소 없이 이어줌
                        };
                            }
                            
                        }
                            
                        }
                        else{
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 2}
                            }
                        };
                        }
                            else{
                                // 앞에 받침 있고 받침 있는 CVC / 냥[냥]꺅
                                if ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))){
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                    return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 8},
                        }
                        };
                                }
                                else {
                                    // 나머지 음소 
                                    return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            new Phoneme { phoneme = $"{thisLastConsonant} -",
                            position = totalDuration - totalDuration / 8},
                        }
                        };
                                }
                                
                            }
                            
                        }
                        
                    }
                    else{
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 온 CVC 음소(받침 있음) / 냥[악]꺅  냥[먁]꺅
                            if ((thisFirstConsonant.Equals("gg")) || (thisFirstConsonant.Equals("dd")) || (thisFirstConsonant.Equals("bb")) || (thisFirstConsonant.Equals("ss")) || (thisFirstConsonant.Equals("jj")) || (thisFirstConsonant.Equals("k")) || (thisFirstConsonant.Equals("t")) || (thisFirstConsonant.Equals("p"))){
                                    // ㄲㄸㅃㅆㅉ ㅋㅌㅍ / - 로 시작해야 함 
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"- {CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }
                        };
                        }
                        else {
                            // 나머지
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength},
                            }
                        };
                        }
                            }
                            
                            
                        
                    }
                    }
                    else if (nextNeighbour?.lyric == "-"){
                        // 둘다 이웃 있고 뒤에 -가 옴
                        if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[아]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            }
                        };
                        }
                        else {
                            // 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("m"))){
                            // 다음 음소가 ㄴㅇㄹㅇ 임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 있는 모음 + 모음 아님 / 냥[앙]냐 냥[냥]냥
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }// -음소 없이 이어줌
                        };
                        }
                            
                        }
                        else{
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }
                        };
                        }
                            else{
                                // 앞에 받침 있는 모음 + 모음 아님 / 냥[앙]꺅  냥[냥]꺅
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - cVCLength,}
                            }
                        };
                            }
                            
                        }
                        
                    }
                    else{
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevSuffix.Equals("")) && (prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 온 모음 + 모음 아님  냥[악]꺅  냥[먁]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        
                    }
                    }
                    else{
                        if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]-
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                        }
                        };
                    }
                        else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                            // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]-
                        
                            return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                                }// -음소 없이 이어줌
                            };
                        }
                        else{
                            // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]-
                            return new Result(){
                            phonemes = new Phoneme[] { 
                                new Phoneme { phoneme = $"{CV}"},
                                new Phoneme { phoneme = $"{cVC}",
                                position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                                }
                            };
                        }
                    }
                }
                else{
                    return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = CV},
                            }
                };
                }
            }
            else{
                
                // [] 표시로 음소 입력하는거 구현
                // - R 등등 구현
                // 로마자 음소 구현
                // 아래 리턴은 임시이다
                var phoneme = lyric;
                return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = phoneme},
                            }
                };
            }
            
           
        }
        
    }

    
}
