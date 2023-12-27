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
using System.Collections;

namespace OpenUtau.Core.Enunu {
    [Phonemizer("Enunu Korean Phonemizer", "ENUNU KO", "EX3", language:"KO")]
    public class EnunuKoreanPhonemizer : EnunuPhonemizer {
        readonly string PhonemizerType = "ENUNU KO";

        public override void SetSinger(USinger singer) {
            this.singer = singer as EnunuSinger;
        }

        public enum ConsonantType{ 
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

            /// <summary>
            /// KO ENUNU phoneme table of first consonants. (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> FIRST_CONSONANTS = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g ", ConsonantType.NORMAL.ToString()}},
                {"ㄲ", new string[2]{"kk ", ConsonantType.FORTIS.ToString()}},
                {"ㄴ", new string[2]{"n ", ConsonantType.NASAL.ToString()}},
                {"ㄷ", new string[2]{"d ", ConsonantType.NORMAL.ToString()}},
                {"ㄸ", new string[2]{"tt ", ConsonantType.FORTIS.ToString()}},
                {"ㄹ", new string[2]{"r ", ConsonantType.LIQUID.ToString()}},
                {"ㅁ", new string[2]{"m ", ConsonantType.NASAL.ToString()}},
                {"ㅂ", new string[2]{"b ", ConsonantType.NORMAL.ToString()}},
                {"ㅃ", new string[2]{"pp ", ConsonantType.FORTIS.ToString()}},
                {"ㅅ", new string[2]{"s ", ConsonantType.NORMAL.ToString()}},
                {"ㅆ", new string[2]{"ss ", ConsonantType.FRICATIVE.ToString()}},
                {"ㅇ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"ㅈ", new string[2]{"j ", ConsonantType.NORMAL.ToString()}},
                {"ㅉ", new string[2]{"jj ", ConsonantType.FORTIS.ToString()}},
                {"ㅊ", new string[2]{"ch ", ConsonantType.ASPIRATE.ToString()}},
                {"ㅋ", new string[2]{"k ", ConsonantType.ASPIRATE.ToString()}},
                {"ㅌ", new string[2]{"t ", ConsonantType.ASPIRATE.ToString()}},
                {"ㅍ", new string[2]{"p ", ConsonantType.ASPIRATE.ToString()}},
                {"ㅎ", new string[2]{"h ", ConsonantType.H.ToString()}},
                {" ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"null", new string[2]{"", ConsonantType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };

            /// <summary>
            /// KO ENUNU phoneme table of middle vowels (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> MIDDLE_VOWELS = new Dictionary<string, string[]>(){
                {"ㅏ", new string[3]{"", "", "a"}},
                {"ㅐ", new string[3]{"", "", "e"}},
                {"ㅑ", new string[3]{"y", " ", "a"}},
                {"ㅒ", new string[3]{"y", " ", "e"}},
                {"ㅓ", new string[3]{"", "", "eo"}},
                {"ㅔ", new string[3]{"", "", "e"}},
                {"ㅕ", new string[3]{"y", " ", "eo"}},
                {"ㅖ", new string[3]{"y", " ", "e"}},
                {"ㅗ", new string[3]{"", "", "o"}},
                {"ㅘ", new string[3]{"w", " ", "a"}},
                {"ㅙ", new string[3]{"w", " ", "e"}},
                {"ㅚ", new string[3]{"w", " ", "e"}},
                {"ㅛ", new string[3]{"y", " ", "o"}},
                {"ㅜ", new string[3]{"", "", "u"}},
                {"ㅝ", new string[3]{"w", " ", "eo"}},
                {"ㅞ", new string[3]{"w", " ", "e"}},
                {"ㅟ", new string[3]{"w", " ", "i"}},
                {"ㅠ", new string[3]{"y", " ", "u"}},
                {"ㅡ", new string[3]{"", "", "eu"}},
                {"ㅢ", new string[3]{"", "", "i"}}, // ㅢ는 ㅣ로 발음
                {"ㅣ", new string[3]{"", "", "i"}},
                {" ", new string[3]{"", "", ""}},
                {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
                };

            /// <summary>
            /// KO ENUNU phoneme table of last consonants. (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> LAST_CONSONANTS = new Dictionary<string, string[]>(){
                 //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
                {"ㄱ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄲ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄳ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄴ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄵ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄶ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄷ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㄹ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄺ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄻ", new string[3]{" M", "1", BatchimType.NASAL_END.ToString()}},
                {"ㄼ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄽ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄾ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄿ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅀ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㅁ", new string[3]{" M", "1", BatchimType.NASAL_END.ToString()}},
                {"ㅂ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅄ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅅ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅆ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅇ", new string[3]{" NG", "3", BatchimType.NG_END.ToString()}},
                {"ㅈ", new string[3]{"T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅊ", new string[3]{"T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅋ", new string[3]{"K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㅌ", new string[3]{"T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅍ", new string[3]{"P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅎ", new string[3]{"T", "1", BatchimType.H_END.ToString()}},
                {" ", new string[3]{"", "", BatchimType.NO_END.ToString()}},
                {"null", new string[3]{"", "", BatchimType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };

        
        protected override EnunuNote[] NoteGroupsToEnunu(Note[][] notes) {
            KoreanPhonemizerUtil.RomanizeNotes(notes, FIRST_CONSONANTS, MIDDLE_VOWELS, LAST_CONSONANTS);
            var result = new List<EnunuNote>();
            int position = 0;
            int index = 0;
            
            while (index < notes.Length) {
                if (position < notes[index][0].position) {
                    result.Add(new EnunuNote {
                        lyric = "R",
                        length = notes[index][0].position - position,
                        noteNum = 60,
                        noteIndex = -1,
                    });
                    position = notes[index][0].position;
                } else {
                    var lyric = notes[index][0].lyric;
                    result.Add(new EnunuNote {
                        lyric = lyric,
                        length = notes[index].Sum(n => n.duration),
                        noteNum = notes[index][0].tone,
                        noteIndex = index,
                    });
                    position += result.Last().length;
                    index++;
                }
            }
            return result.ToArray();
        }
    }
}
