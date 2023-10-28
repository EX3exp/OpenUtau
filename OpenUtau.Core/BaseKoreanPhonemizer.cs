using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core {
    public abstract class BaseKoreanPhonemizer : Phonemizer {
        // Can process Phoneme variation.
        // Can find Alias in oto, including Voice color etc.
        // Can manage .ini configuring.
        // Can generate phonemes according to Phoneme hints.
        protected USinger singer;
        protected int vcLength = 120; // TODO
        protected int vcLengthShort = 90;
        protected Hanguel hangeul = new Hanguel();


        public override void SetSinger(USinger singer) => this.singer = singer;
        public static string? findInOto(USinger singer, string phoneme, Note note, bool nullIfNotFound = false) {
            // 음소와 노트를 입력받고, 다음계 및 보이스컬러 에일리어스를 적용한다. 
            // nullIfNotFound가 true이면 음소가 찾아지지 않을 때 음소가 아닌 null을 리턴한다.
            // nullIfNotFound가 false면 음소가 찾아지지 않을 때 그대로 음소를 반환
            string phonemeToReturn;
            string color = string.Empty;
            int toneShift = 0;
            int? alt = null;
            if (phoneme.Equals("")) {
                return phoneme;
            }

            if (singer.TryGetMappedOto(phoneme + alt, note.tone + toneShift, color, out var otoAlt)) {
                phonemeToReturn = otoAlt.Alias;
            } else if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phonemeToReturn = oto.Alias;
            } else if (singer.TryGetMappedOto(phoneme, note.tone, color, out oto)) {
                phonemeToReturn = oto.Alias;
            } else if (nullIfNotFound) {
                phonemeToReturn = null;
            } else {
                phonemeToReturn = phoneme;
            }

            return phonemeToReturn;
        }
        
        /// <summary>
        ///  for ini generate & read
        /// </summary>

        public virtual Result convertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            // All child KO Phonemizer have to do is to implementing this (1)
            // below return is Dummy
            return new Result() {
                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $""},
                            }
            };
        }

        public virtual Result generateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            // All child KO Phonemizer have to do is to implementing this (2)
            // below return is Dummy
            return new Result() {
                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $""},
                            }
            };
        }

        /// <summary>
        /// <param name="firstPhoneme"></param>
        /// <param name="secondPhoneme"></param>
        /// <param name="totalDuration"></param>
        /// <param name="totalDurationDivider"></param>
        /// </summary>
        public Result generateResult(String firstPhoneme, String secondPhoneme, int totalDuration, int secondPhonemePosition, int totalDurationDivider=3){
            return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = firstPhoneme },
                            new Phoneme { phoneme = secondPhoneme,
                            position = totalDuration - Math.Min(totalDuration / totalDurationDivider, secondPhonemePosition)},
                            }
                    };
        }
        public Result generateResult(String firstPhoneme){
            return new Result() {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = firstPhoneme },
                            }
                    };
        }

        public Result generateResult(String firstPhoneme, String secondPhoneme, String thirdPhoneme, int totalDuration, int secondPhonemePosition, int secondTotalDurationDivider=3, int thirdTotalDurationDivider=8){
            return new Result() {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = firstPhoneme},
                            new Phoneme { phoneme = secondPhoneme,
                            position = totalDuration - Math.Min(totalDuration / secondTotalDurationDivider, secondPhonemePosition)},
                            new Phoneme { phoneme = thirdPhoneme,
                            position = totalDuration - totalDuration / thirdTotalDurationDivider},
                            }// -음소 있이 이어줌
                            };
        }
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            /// it does: - generate phonemes according to phoneme hints (each phonemes should be separated by ",". (Example: "a, a i, ya"))
            /// it does not: - generate phonemes (so should implement convertPhonemes() Method in child class.)
            ///              - generate Endsounds (so should implement generateEndSound() Method in child class.)
            Hanguel hanguel = new Hanguel();

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);

            if (phoneticHint != null) {
                // if there are phonetic hint
                // 발음 힌트가 있음 
                // 냥[nya2, ang]
                string[] phoneticHints = phoneticHint.Split(','); // phonemes are seperated by ','.
                int phoneticHintsLength = phoneticHints.Length;



                Phoneme[] phonemes = new Phoneme[phoneticHintsLength];

                Dictionary<string, string> VVdictionary = new Dictionary<string, string>() { };

                string[] VVsource = new string[] { "a", "i", "u", "e", "o", "eo", "eu" };


                for (int i = 0; i < 7; i++) {
                    // VV 딕셔너리를 채운다
                    // 나중에 발음기호에 ["a a"]를 입력하고 만일 음원에게 "a a"가 없을 경우, 자동으로 VVDictionary에서 "a a"에 해당하는 값인 "a"를 호출해 사용
                    // (반대도 똑같이 적용)

                    // VVDictionary 예시: {"a a", "a"} ...
                    for (int j = 6; j >= 0; j--) {
                        VVdictionary[$"{VVsource[i]} {VVsource[j]}"] = $"{VVsource[j]}"; // CV/CVC >> CBNN 호환용
                        VVdictionary[$"{VVsource[j]}"] = $"{VVsource[i]} {VVsource[j]}"; // CBNN >> CV/CVC 호환용
                    }

                }

                for (int i = 0; i < phoneticHintsLength; i++) {
                    string? alias = findInOto(singer, phoneticHints[i].Trim(), note, true); // alias if exists, otherwise null

                    if (alias != null) {
                        // 발음기호에 입력된 phoneme이 음원에 존재함

                        if (i == 0) {
                            // first syllable
                            phonemes[i] = new Phoneme { phoneme = alias };
                        } else if ((i == phoneticHintsLength - 1) && ((phoneticHints[i].Trim().EndsWith('-')) || phoneticHints[i].Trim().EndsWith('R'))) {
                            // 마지막 음소이고 끝음소(ex: a -, a R)일 경우, VCLengthShort에 맞춰 음소를 배치
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - Math.Min(vcLengthShort, totalDuration / 8)
                                // 8등분한 길이로 끝에 숨소리 음소 배치, n등분했을 때의 음소 길이가 이보다 작다면 n등분했을 때의 길이로 간다
                            };
                        } else if (phoneticHintsLength == 2) {
                            // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                            // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - totalDuration / 3
                                // 3등분해서 음소가 배치됨
                            };
                        } else {
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                                // 균등하게 n등분해서 음소가 배치됨
                            };
                        }
                    } else if (VVdictionary.ContainsKey(phoneticHints[i].Trim())) {
                        // 입력 실패한 음소가 VV 혹은 V일 때
                        if (phoneticHintsLength == 2) {
                            // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                            // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                            phonemes[i] = new Phoneme {
                                phoneme = findInOto(singer, VVdictionary[phoneticHints[i].Trim()], note),
                                position = totalDuration - totalDuration / 3
                                // 3등분해서 음소가 배치됨
                            };
                        } else {
                            phonemes[i] = new Phoneme {
                                phoneme = findInOto(singer, VVdictionary[phoneticHints[i].Trim()], note),
                                position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                                // 균등하게 n등분해서 음소가 배치됨
                            };
                        }
                    } else {
                        // 그냥 음원에 음소가 없음
                        phonemes[i] = new Phoneme {
                            phoneme = phoneticHints[i].Trim(),
                            position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                            // 균등하게 n등분해서 음소가 배치됨
                        };
                    }

                }

                return new Result() {
                    phonemes = phonemes
                };
            } else if (hanguel.isHangeul(lyric) && (!lyric.Equals("-")) && (!lyric.Equals("R"))) {
                return convertPhonemes(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
            } else {
                return generateEndSound(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
            }
        }
    }
}