// This file is part of Companion Cube project
//
// Copyright 2018 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

// all stuff below is done by hand
// hours of work
namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Raw unicode block data.
    /// </summary>
    public struct RawBlockData
    {
        /// <summary>
        /// Gets the start codepoint of this character block.
        /// </summary>
        [JsonProperty("start_codepoint")]
        public string StartCodepoint { get; private set; }

        /// <summary>
        /// Gets the start of this character block.
        /// </summary>
        [JsonProperty("start")]
        public uint Start { get; private set; }

        /// <summary>
        /// Gets the end codepoint of this character block.
        /// </summary>
        [JsonProperty("end_codepoint")]
        public string EndCodepoint { get; private set; }

        /// <summary>
        /// Gets the end of this character block.
        /// </summary>
        [JsonProperty("end")]
        public uint End { get; private set; }

        /// <summary>
        /// Gets the name of this character block.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }
    }

    /// <summary>
    /// Raw unihan character data.
    /// </summary>
    public struct RawUnihanData
    {
        /// <summary>
        /// Gets the codepoint corresponding to this unihan character.
        /// </summary>
        [JsonProperty("codepoint")]
        public string Codepoint { get; private set; }

        /// <summary>
        /// Gets the cantonese pronounciation for this character using the jyutping romanization.
        /// </summary>
        [JsonProperty("cantonese")]
        public string Cantonese { get; private set; }

        /// <summary>
        /// Gets an English definition for this character. Definitions are for modern written Chinese and are usually (but not always) the same as the definition in other Chinese 
        /// dialects or non-Chinese languages. In some cases, synonyms are indicated. Fuller variant information can be found using the various variant fields.
        /// </summary>
        [JsonProperty("definition")]
        public string Definition { get; private set; }

        /// <summary>
        /// Gets the modern Korean pronunciation(s) for this character in Hangul.
        /// </summary>
        [JsonProperty("hangul")]
        public string Hangul { get; private set; }

        /// <summary>
        /// Gets the pronunciations and frequencies of this character, based in part on those appearing in Xiandai Hanyu Pinlu Cidian.
        /// </summary>
        [JsonProperty("hanyu_pinlu")]
        public string HanyuPinlu { get; private set; }

        /// <summary>
        /// Gets the pronunciations and frequencies of this character, based in part on those appearing in HDZ.
        /// </summary>
        [JsonProperty("hanyu_pinyin")]
        public string HanyuPinyin { get; private set; }

        /// <summary>
        /// Gets the Japanese pronunciation(s) of this character.
        /// </summary>
        [JsonProperty("japanese_kun")]
        public string JapaneseKun { get; private set; }

        /// <summary>
        /// Gets the Sino-Japanese pronunciation(s) of this character.
        /// </summary>
        [JsonProperty("japanese_on")]
        public string JapaneseOn { get; private set; }

        /// <summary>
        /// Gets the Korean pronunciation(s) of this character, using the Yale romanization system.
        /// </summary>
        [JsonProperty("korean")]
        public string Korean { get; private set; }

        /// <summary>
        /// Gets the most customary pinyin reading for this character. When there are two values, then the first is preferred for zh-Hans (CN) and the second is preferred for 
        /// zh-Hant (TW). When there is only one value, it is appropriate for both.
        /// </summary>
        [JsonProperty("mandarin")]
        public string Mandarin { get; private set; }

        /// <summary>
        /// Gets the Tang dynasty pronunciation(s) of this character, derived from or consistent with T’ang Poetic Vocabulary by Hugh M. Stimson, Far Eastern Publications, Yale 
        /// Univ. 1976. An asterisk indicates that the word or morpheme represented in toto or in part by the given character with the given reading occurs more than four times 
        /// in the seven hundred poems covered.
        /// </summary>
        [JsonProperty("tang")]
        public string Tang { get; private set; }

        /// <summary>
        /// Gets the character’s pronunciation(s) in Quốc ngữ.
        /// </summary>
        [JsonProperty("vietnamese")]
        public string Vietnamese { get; private set; }

        /// <summary>
        /// Gets one or more Hànyǔ Pīnyīn readings as given in the Xiàndài Hànyǔ Cídiǎn.
        /// </summary>
        [JsonProperty("xhc1983")]
        public string Xhc1983 { get; private set; }
    }

    /// <summary>
    /// Raw unicode character data.
    /// </summary>
    public struct RawCharacterData
    {
        [JsonProperty("codepoint")]
        public string Codepoint { get; private set; }

        [JsonProperty("value")]
        public uint Value { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("general_category")]
        public string GeneralCategory { get; private set; }

        [JsonProperty("canonical_combining_class")]
        public string CanonicalCombiningClass { get; private set; }

        [JsonProperty("bidi_class")]
        public string BidiClass { get; private set; }

        [JsonProperty("decomposition_type_and_mapping")]
        public string DecompositionTypeAndMapping { get; private set; }

        [JsonProperty("numeric_value_decimal")]
        public string NumericValueDecimal { get; private set; }

        [JsonProperty("numeric_value_digit")]
        public string NumericValueDigit { get; private set; }

        [JsonProperty("numeric_value_numeric")]
        public string NumericValueNumeric { get; private set; }

        [JsonProperty("bidi_mirrored")]
        public string BidiMirrored { get; private set; }

        [JsonProperty("old_unicode_name")]
        public string OldUnicodeName { get; private set; }

        [JsonProperty("iso_comment")]
        public string IsoComment { get; private set; }

        [JsonProperty("simple_uppercase_mapping")]
        public string SimpleUppercaseMapping { get; private set; }

        [JsonProperty("simple_lowercase_mapping")]
        public string SimpleLowercaseMapping { get; private set; }

        [JsonProperty("simple_titlecase_mapping")]
        public string SimpleTitlecaseMapping { get; private set; }

        [JsonProperty("block_name")]
        public string BlockName { get; private set; }
    }

    /// <summary>
    /// Raw unicode data.
    /// </summary>
    public struct RawUnicodeData
    {
        /// <summary>
        /// Gets the blocks in this dataset.
        /// </summary>
        [JsonProperty("blocks")]
        public IEnumerable<RawBlockData> Blocks { get; private set; }

        /// <summary>
        /// Gets the unihan data in this dataset.
        /// </summary>
        [JsonProperty("unihan")]
        public IEnumerable<RawUnihanData> Unihan { get; private set; }

        /// <summary>
        /// Gets the characters in this dataset.
        /// </summary>
        [JsonProperty("characters")]
        public IEnumerable<RawCharacterData> Characters { get; private set; }
    }

    /// <summary>
    /// Represents a string value an enum value can take.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumValueAttribute : Attribute
    {
        /// <summary>
        /// Gets the value for this enum value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the description for this enum value.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Defines a string value for this enum value.
        /// </summary>
        /// <param name="value">String representation.</param>
        /// <param name="desc">Description of this value.</param>
        public EnumValueAttribute(string value, string desc)
        {
            this.Value = value;
            this.Description = desc;
        }
    }

    /// <summary>
    /// Represents a unicode character category.
    /// </summary>
    [Flags]
    public enum UnicodeCategory : int
    {
        /// <summary>
        /// Unknown category.
        /// </summary>
        [EnumValue("", "Unknown category")]
        Unknown = 0,

        /// <summary>
        /// Uppercase letter.
        /// </summary>
        [EnumValue("Lu", "Uppercase letter")]
        UppercaseLetter = 0b0000_0000_0000_0000_0000_0000_0000_0001,

        /// <summary>
        /// Lowercase letter.
        /// </summary>
        [EnumValue("Ll", "Lowercase letter")]
        LowercaseLetter = 0b0000_0000_0000_0000_0000_0000_0000_0010,

        /// <summary>
        /// Digraphic character, with first part uppercase.
        /// </summary>
        [EnumValue("Lt", "Digraphic character, with first part uppercase")]
        TitlecaseLetter = 0b0000_0000_0000_0000_0000_0000_0000_0100,

        /// <summary>
        /// Combination of <see cref="UppercaseLetter"/>, <see cref="LowercaseLetter"/>, and <see cref="TitlecaseLetter"/>.
        /// </summary>
        [EnumValue("LC", "Uppercase, lowercase, or digraphic character")]
        CasedLetter = UppercaseLetter | LowercaseLetter | TitlecaseLetter,

        /// <summary>
        /// Modified letter.
        /// </summary>
        [EnumValue("Lm", "Modifier letter")]
        ModifierLetter = 0b0000_0000_0000_0000_0000_0000_0000_1000,

        /// <summary>
        /// Other letters, including syllables and ideographs.
        /// </summary>
        [EnumValue("Lo", "Other letters, including syllables and ideographs")]
        OtherLetter = 0b0000_0000_0000_0000_0000_0000_0001_0000,

        /// <summary>
        /// Combination of <see cref="UppercaseLetter"/>, <see cref="LowercaseLetter"/>, <see cref="TitlecaseLetter"/>, <see cref="ModifierLetter"/>, and <see cref="OtherLetter"/>.
        /// </summary>
        [EnumValue("L", "Any letter")]
        Letter = UppercaseLetter | LowercaseLetter | TitlecaseLetter | ModifierLetter | OtherLetter,

        /// <summary>
        /// Nonspacing combining mark (zero advance width).
        /// </summary>
        [EnumValue("Mn", "Nonspacing combining mark (zero advance width)")]
        NonspacingMark = 0b0000_0000_0000_0000_0000_0000_0010_0000,

        /// <summary>
        /// Spacing combining mark (positive advance width).
        /// </summary>
        [EnumValue("Mc", "Spacing combining mark (positive advance width)")]
        SpacingMark = 0b0000_0000_0000_0000_0000_0000_0100_0000,

        /// <summary>
        /// Enclosing combining mark.
        /// </summary>
        [EnumValue("Me", "Enclosing combining mark")]
        EnclosingMark = 0b0000_0000_0000_0000_0000_0000_1000_0000,

        /// <summary>
        /// Combination of <see cref="NonspacingMark"/>, <see cref="SpacingMark"/>, and <see cref="EnclosingMark"/>.
        /// </summary>
        [EnumValue("M", "Any mark")]
        Mark = NonspacingMark | SpacingMark | EnclosingMark,

        /// <summary>
        /// Decimal digit.
        /// </summary>
        [EnumValue("Nd", "Decimal digit")]
        DecimalNumber = 0b0000_0000_0000_0000_0000_0001_0000_0000,

        /// <summary>
        /// Letterlike numeric character.
        /// </summary>
        [EnumValue("Nl", "Letterlike numeric character")]
        LetterNumber = 0b0000_0000_0000_0000_0000_0010_0000_0000,

        /// <summary>
        /// Numeric character of other type.
        /// </summary>
        [EnumValue("No", "Numeric character of other type")]
        OtherNumber = 0b0000_0000_0000_0000_0000_0100_0000_0000,

        /// <summary>
        /// Combination of <see cref="DecimalNumber"/>, <see cref="LetterNumber"/>, and <see cref="OtherNumber"/>.
        /// </summary>
        [EnumValue("N", "Any number")]
        Number = DecimalNumber | LetterNumber | OtherNumber,

        /// <summary>
        /// Connecting punctuation mark, like a tie.
        /// </summary>
        [EnumValue("Pc", "Connecting punctuation mark, like a tie")]
        ConnectorPunctuation = 0b0000_0000_0000_0000_0000_1000_0000_0000,

        /// <summary>
        /// Dash or hyphen punctuation mark.
        /// </summary>
        [EnumValue("Pd", "Dash or hyphen punctuation mark")]
        DashPunctuation = 0b0000_0000_0000_0000_0001_0000_0000_0000,

        /// <summary>
        /// Opening punctuation mark (of a pair).
        /// </summary>
        [EnumValue("Ps", "Opening punctuation mark (of a pair)")]
        OpenPunctuation = 0b0000_0000_0000_0000_0010_0000_0000_0000,

        /// <summary>
        /// Closing punctuation mark (of a pair).
        /// </summary>
        [EnumValue("Pe", "Closing punctuation mark (of a pair)")]
        ClosePunctuation = 0b0000_0000_0000_0000_0100_0000_0000_0000,

        /// <summary>
        /// Initial quotation mark.
        /// </summary>
        [EnumValue("Pi", "Initial quotation mark")]
        InitialPunctuation = 0b0000_0000_0000_0000_1000_0000_0000_0000,

        /// <summary>
        /// Final quotation mark.
        /// </summary>
        [EnumValue("Pf", "Final quotation mark")]
        FinalPunctuation = 0b0000_0000_0000_0001_0000_0000_0000_0000,

        /// <summary>
        /// Punctuation mark of another type
        /// </summary>
        [EnumValue("Po", "Punctuation mark of another type")]
        OtherPunctuation = 0b0000_0000_0000_0010_0000_0000_0000_0000,

        /// <summary>
        /// Combination of <see cref="ConnectorPunctuation"/>, <see cref="DashPunctuation"/>, <see cref="OpenPunctuation"/>, <see cref="ClosePunctuation"/>, <see cref="InitialPunctuation"/>, <see cref="FinalPunctuation"/>, and <see cref="OtherPunctuation"/>.
        /// </summary>
        [EnumValue("P", "Any punctuation")]
        Punctuation = ConnectorPunctuation | DashPunctuation | OpenPunctuation | ClosePunctuation | InitialPunctuation | FinalPunctuation | OtherPunctuation,

        /// <summary>
        /// Symbol of mathematical use.
        /// </summary>
        [EnumValue("Sm", "Symbol of mathematical use")]
        MathSymbol = 0b0000_0000_0000_0100_0000_0000_0000_0000,

        /// <summary>
        /// Currency sign.
        /// </summary>
        [EnumValue("Sc", "Currency sign")]
        CurrencySymbol = 0b0000_0000_0000_1000_0000_0000_0000_0000,

        /// <summary>
        /// Non-letterlike modifier symbol.
        /// </summary>
        [EnumValue("Sk", "Non-letterlike modifier symbol")]
        ModifierSymbol = 0b0000_0000_0001_0000_0000_0000_0000_0000,

        /// <summary>
        /// Symbol of other type.
        /// </summary>
        [EnumValue("So", "Symbol of other type")]
        OtherSymbol = 0b0000_0000_0010_0000_0000_0000_0000_0000,

        /// <summary>
        /// Combination of <see cref="MathSymbol"/>, <see cref="CurrencySymbol"/>, <see cref="ModifierSymbol"/>, and <see cref="OtherSymbol"/>.
        /// </summary>
        [EnumValue("S", "Any symbol")]
        Symbol = MathSymbol | CurrencySymbol | ModifierSymbol | OtherSymbol,

        /// <summary>
        /// Space character (of various non-zero widths).
        /// </summary>
        [EnumValue("Zs", "Space character (of various non-zero widths)")]
        SpaceSeparator = 0b0000_0000_0100_0000_0000_0000_0000_0000,

        /// <summary>
        /// U+2028 LINE SEPARATOR only.
        /// </summary>
        [EnumValue("Zl", "U+2028 LINE SEPARATOR only")]
        LineSeparator = 0b0000_0000_1000_0000_0000_0000_0000_0000,

        /// <summary>
        /// U+2029 PARAGRAPH SEPARATOR only.
        /// </summary>
        [EnumValue("Zp", "U+2029 PARAGRAPH SEPARATOR only")]
        ParagraphSeparator = 0b0000_0001_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Combination of <see cref="SpaceSeparator"/>, <see cref="LineSeparator"/>, and <see cref="ParagraphSeparator"/>.
        /// </summary>
        [EnumValue("Z", "Any separator")]
        Separator = SpaceSeparator | LineSeparator | ParagraphSeparator,

        /// <summary>
        /// C0 or C1 control code.
        /// </summary>
        [EnumValue("Cc", "C0 or C1 control code")]
        Control = 0b0000_0010_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Format control character.
        /// </summary>
        [EnumValue("Cf", "Format control character")]
        Format = 0b0000_0100_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Surrogate code point.
        /// </summary>
        [EnumValue("Cs", "Surrogate code point")]
        Surrogate = 0b0000_1000_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Private-use character.
        /// </summary>
        [EnumValue("Co", "Private-use character")]
        PrivateUse = 0b0001_0000_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Reserved unassigned code point or a noncharacter.
        /// </summary>
        [EnumValue("Cn", "Reserved unassigned code point or a noncharacter")]
        Unassigned = 0b0010_0000_0000_0000_0000_0000_0000_0000,

        /// <summary>
        /// Combination of <see cref="Control"/>, <see cref="Format"/>, <see cref="Surrogate"/>, <see cref="PrivateUse"/>, and <see cref="Unassigned"/>.
        /// </summary>
        [EnumValue("C", "Any other")]
        Other = Control | Format | Surrogate | PrivateUse | Unassigned
    }

    /// <summary>
    /// Represents a unicode combining class, used to order characters.
    /// </summary>
    public enum UnicodeCombiningClass : byte
    {
        /// <summary>
        /// Spacing and enclosing marks; also many vowel and consonant signs, even if nonspacing.
        /// </summary>
        [EnumValue("0", "Spacing and enclosing marks; also many vowel and consonant signs, even if nonspacing")]
        NotReordered = 0,

        /// <summary>
        /// Marks which overlay a base letter or symbol.
        /// </summary>
        [EnumValue("1", "Marks which overlay a base letter or symbol")]
        Overlay = 1,

        /// <summary>
        /// Diacritic nukta marks in Brahmi-derived scripts.
        /// </summary>
        [EnumValue("7", "Diacritic nukta marks in Brahmi-derived scripts")]
        Nukta = 7,

        /// <summary>
        /// Hiragana/Katakana voicing marks.
        /// </summary>
        [EnumValue("8", "Hiragana/Katakana voicing marks")]
        KanaVoicing = 8,

        /// <summary>
        /// Viramas.
        /// </summary>
        [EnumValue("9", "Viramas")]
        Virama = 9,

        /// <summary>
        /// Fixed position 10.
        /// </summary>
        [EnumValue("10", "Fixed position 10")]
        FixedPosition10 = 10,

        /// <summary>
        /// Fixed position 11.
        /// </summary>
        [EnumValue("11", "Fixed position 11")]
        FixedPosition11 = 11,

        /// <summary>
        /// Fixed position 12.
        /// </summary>
        [EnumValue("12", "Fixed position 12")]
        FixedPosition12 = 12,

        /// <summary>
        /// Fixed position 13.
        /// </summary>
        [EnumValue("13", "Fixed position 13")]
        FixedPosition13 = 13,

        /// <summary>
        /// Fixed position 14.
        /// </summary>
        [EnumValue("14", "Fixed position 14")]
        FixedPosition14 = 14,

        /// <summary>
        /// Fixed position 15.
        /// </summary>
        [EnumValue("15", "Fixed position 15")]
        FixedPosition15 = 15,

        /// <summary>
        /// Fixed position 16.
        /// </summary>
        [EnumValue("16", "Fixed position 16")]
        FixedPosition16 = 16,

        /// <summary>
        /// Fixed position 17.
        /// </summary>
        [EnumValue("17", "Fixed position 17")]
        FixedPosition17 = 17,

        /// <summary>
        /// Fixed position 18.
        /// </summary>
        [EnumValue("18", "Fixed position 18")]
        FixedPosition18 = 18,

        /// <summary>
        /// Fixed position 19.
        /// </summary>
        [EnumValue("19", "Fixed position 19")]
        FixedPosition19 = 19,

        /// <summary>
        /// Fixed position 20.
        /// </summary>
        [EnumValue("20", "Fixed position 20")]
        FixedPosition20 = 20,

        /// <summary>
        /// Fixed position 21.
        /// </summary>
        [EnumValue("21", "Fixed position 21")]
        FixedPosition21 = 21,

        /// <summary>
        /// Fixed position 22.
        /// </summary>
        [EnumValue("22", "Fixed position 22")]
        FixedPosition22 = 22,

        /// <summary>
        /// Fixed position 23.
        /// </summary>
        [EnumValue("23", "Fixed position 23")]
        FixedPosition23 = 23,

        /// <summary>
        /// Fixed position 24.
        /// </summary>
        [EnumValue("24", "Fixed position 24")]
        FixedPosition24 = 24,

        /// <summary>
        /// Fixed position 25.
        /// </summary>
        [EnumValue("25", "Fixed position 25")]
        FixedPosition25 = 25,

        /// <summary>
        /// Fixed position 26.
        /// </summary>
        [EnumValue("26", "Fixed position 26")]
        FixedPosition26 = 26,

        /// <summary>
        /// Fixed position 27.
        /// </summary>
        [EnumValue("27", "Fixed position 27")]
        FixedPosition27 = 27,

        /// <summary>
        /// Fixed position 28.
        /// </summary>
        [EnumValue("28", "Fixed position 28")]
        FixedPosition28 = 28,

        /// <summary>
        /// Fixed position 29.
        /// </summary>
        [EnumValue("29", "Fixed position 29")]
        FixedPosition29 = 29,

        /// <summary>
        /// Fixed position 30.
        /// </summary>
        [EnumValue("30", "Fixed position 30")]
        FixedPosition30 = 30,

        /// <summary>
        /// Fixed position 31.
        /// </summary>
        [EnumValue("31", "Fixed position 31")]
        FixedPosition31 = 31,

        /// <summary>
        /// Fixed position 32.
        /// </summary>
        [EnumValue("32", "Fixed position 32")]
        FixedPosition32 = 32,

        /// <summary>
        /// Fixed position 33.
        /// </summary>
        [EnumValue("33", "Fixed position 33")]
        FixedPosition33 = 33,

        /// <summary>
        /// Fixed position 34.
        /// </summary>
        [EnumValue("34", "Fixed position 34")]
        FixedPosition34 = 34,

        /// <summary>
        /// Fixed position 35.
        /// </summary>
        [EnumValue("35", "Fixed position 35")]
        FixedPosition35 = 35,

        /// <summary>
        /// Fixed position 36.
        /// </summary>
        [EnumValue("36", "Fixed position 36")]
        FixedPosition36 = 36,

        /// <summary>
        /// Fixed position 84.
        /// </summary>
        [EnumValue("84", "Fixed position 84")]
        FixedPosition84 = 84,

        /// <summary>
        /// Fixed position 91.
        /// </summary>
        [EnumValue("91", "Fixed position 91")]
        FixedPosition91 = 91,

        /// <summary>
        /// Fixed position 103.
        /// </summary>
        [EnumValue("103", "Fixed position 103")]
        FixedPosition103 = 103,

        /// <summary>
        /// Fixed position 107.
        /// </summary>
        [EnumValue("107", "Fixed position 107")]
        FixedPosition107 = 107,

        /// <summary>
        /// Fixed position 118.
        /// </summary>
        [EnumValue("118", "Fixed position 118")]
        FixedPosition118 = 118,

        /// <summary>
        /// Fixed position 122.
        /// </summary>
        [EnumValue("122", "Fixed position 122")]
        FixedPosition122 = 122,

        /// <summary>
        /// Fixed position 129.
        /// </summary>
        [EnumValue("129", "Fixed position 129")]
        FixedPosition129 = 129,

        /// <summary>
        /// Fixed position 130.
        /// </summary>
        [EnumValue("130", "Fixed position 130")]
        FixedPosition130 = 130,

        /// <summary>
        /// Fixed position 132.
        /// </summary>
        [EnumValue("132", "Fixed position 132")]
        FixedPosition132 = 132,

        /// <summary>
        /// Fixed position 133.
        /// </summary>
        [EnumValue("133", "Fixed position 133")]
        FixedPosition133 = 133,

        /// <summary>
        /// Marks attached at the bottom left.
        /// </summary>
        [EnumValue("200", "Marks attached at the bottom left")]
        AttachedBelowLeft = 200,

        /// <summary>
        /// Marks attached directly below.
        /// </summary>
        [EnumValue("202", "Marks attached directly below")]
        AttachedBelow = 202,

        /// <summary>
        /// Marks attached at the bottom right.
        /// </summary>
        [EnumValue("204", "Marks attached at the bottom right")]
        AttachedBelowRight = 204,

        /// <summary>
        /// Marks attached to the left.
        /// </summary>
        [EnumValue("208", "Marks attached to the left")]
        AttachedLeft = 208,

        /// <summary>
        /// Marks attached to the right.
        /// </summary>
        [EnumValue("210", "Marks attached to the right")]
        AttachedRight = 210,

        /// <summary>
        /// Marks attached at the top left.
        /// </summary>
        [EnumValue("212", "Marks attached at the top left")]
        AttachedAboveLeft = 212,

        /// <summary>
        /// Marks attached directly above.
        /// </summary>
        [EnumValue("214", "Marks attached directly above")]
        AttachedAbove = 214,

        /// <summary>
        /// Marks attached at the top right.
        /// </summary>
        [EnumValue("216", "Marks attached at the top right")]
        AttachedAboveRight = 216,

        /// <summary>
        /// Distinct marks at the bottom left.
        /// </summary>
        [EnumValue("218", "Distinct marks at the bottom left")]
        BelowLeft = 218,

        /// <summary>
        /// Distinct marks directly below.
        /// </summary>
        [EnumValue("220", "Distinct marks directly below")]
        Below = 220,

        /// <summary>
        /// Distinct marks at the bottom right.
        /// </summary>
        [EnumValue("222", "Distinct marks at the bottom right")]
        BelowRight = 222,

        /// <summary>
        /// Distinct marks to the left.
        /// </summary>
        [EnumValue("224", "Distinct marks to the left")]
        Left = 224,

        /// <summary>
        /// Distinct marks to the right.
        /// </summary>
        [EnumValue("226", "Distinct marks to the right")]
        Right = 226,

        /// <summary>
        /// Distinct marks at the top left.
        /// </summary>
        [EnumValue("228", "Distinct marks at the top left")]
        AboveLeft = 228,

        /// <summary>
        /// Distinct marks directly above.
        /// </summary>
        [EnumValue("230", "Distinct marks directly above")]
        Above = 230,

        /// <summary>
        /// Distinct marks at the top right.
        /// </summary>
        [EnumValue("232", "Distinct marks at the top right")]
        AboveRight = 232,

        /// <summary>
        /// Distinct marks subtending two bases.
        /// </summary>
        [EnumValue("233", "Distinct marks subtending two bases")]
        DoubleBelow = 233,

        /// <summary>
        /// Distinct marks extending above two bases.
        /// </summary>
        [EnumValue("234", "Distinct marks extending above two bases")]
        DoubleAbove = 234,

        /// <summary>
        /// Greek iota subscript only.
        /// </summary>
        [EnumValue("240", "Greek iota subscript only")]
        IotaSubscript = 240
    }

    /// <summary>
    /// Represents a unicode bidirectionality class, used when displaying characters.
    /// </summary>
    [Flags]
    public enum UnicodeBidirectionalityClass : int
    {
        /// <summary>
        /// Unknown class.
        /// </summary>
        [EnumValue("", "Unknown class")]
        Unknown = 0,

        /// <summary>
        /// Any strong left-to-right character.
        /// </summary>
        [EnumValue("L", "Any strong left-to-right character")]
        LeftToRight = 0b0000_0000_0000_0000_0000_0000_0000_0001,

        /// <summary>
        /// Any strong right-to-left (non-Arabic-type) character.
        /// </summary>
        [EnumValue("R", "Any strong right-to-left (non-Arabic-type) character")]
        RightToLeft = 0b0000_0000_0000_0000_0000_0000_0000_0010,

        /// <summary>
        /// Any strong right-to-left (Arabic-type) character.
        /// </summary>
        [EnumValue("AL", "Any strong right-to-left (Arabic-type) character")]
        ArabicLetter = 0b0000_0000_0000_0000_0000_0000_0000_0100,

        /// <summary>
        /// Any strong character.
        /// </summary>
        [EnumValue("L,R,AL", "Any strong character")]
        StrongTypes = LeftToRight | RightToLeft | ArabicLetter,

        /// <summary>
        /// Any ASCII digit or Eastern Arabic-Indic digit.
        /// </summary>
        [EnumValue("EN", "Any ASCII digit or Eastern Arabic-Indic digit")]
        EuropeanNumber = 0b0000_0000_0000_0000_0000_0000_0000_1000,

        /// <summary>
        /// Plus and minus signs.
        /// </summary>
        [EnumValue("ES", "Plus and minus signs")]
        EuropeanSeparator = 0b0000_0000_0000_0000_0000_0000_0001_0000,

        /// <summary>
        /// Terminator in a numeric format context, includes currency signs.
        /// </summary>
        [EnumValue("ET", "Terminator in a numeric format context, includes currency signs")]
        EuropeanTerminator = 0b0000_0000_0000_0000_0000_0000_0010_0000,

        /// <summary>
        /// Any Arabic-Indic digit.
        /// </summary>
        [EnumValue("AN", "Any Arabic-Indic digit")]
        ArabicNumber = 0b0000_0000_0000_0000_0000_0000_0100_0000,

        /// <summary>
        /// Commas, colons, and slashes.
        /// </summary>
        [EnumValue("CS", "Commas, colons, and slashes")]
        CommonSeparator = 0b0000_0000_0000_0000_0000_0000_1000_0000,

        /// <summary>
        /// Any nonspacing mark.
        /// </summary>
        [EnumValue("NSM", "Any nonspacing mark")]
        NonSpacingMark = 0b0000_0000_0000_0000_0000_0001_0000_0000,

        /// <summary>
        /// Most format characters, control codes, or noncharacters.
        /// </summary>
        [EnumValue("BN", "Most format characters, control codes, or noncharacters")]
        BoundaryNeutral = 0b0000_0000_0000_0000_0000_0010_0000_0000,

        /// <summary>
        /// Any weak character.
        /// </summary>
        [EnumValue("EN,ES,ET,AN,CS,NSM,BN", "Any weak character")]
        WeakTypes = EuropeanNumber | EuropeanSeparator | EuropeanTerminator | ArabicNumber | CommonSeparator | NonSpacingMark | BoundaryNeutral,

        /// <summary>
        /// Various newline characters.
        /// </summary>
        [EnumValue("B", "Various newline characters")]
        ParagraphSeparator = 0b0000_0000_0000_0000_0000_0100_0000_0000,

        /// <summary>
        /// Various segment-related control codes.
        /// </summary>
        [EnumValue("S", "Various segment-related control codes")]
        SegmentSeparator = 0b0000_0000_0000_0000_0000_1000_0000_0000,

        /// <summary>
        /// Spaces.
        /// </summary>
        [EnumValue("WS", "Spaces.")]
        WhiteSpace = 0b0000_0000_0000_0000_0001_0000_0000_0000,

        /// <summary>
        /// Most other symbols and punctuation marks.
        /// </summary>
        [EnumValue("ON", "Most other symbols and punctuation marks")]
        OtherNeutral = 0b0000_0000_0000_0000_0010_0000_0000_0000,

        /// <summary>
        /// Any neutral character.
        /// </summary>
        [EnumValue("B,S,WS,ON", "Any neutral character")]
        NeutralTypes = ParagraphSeparator | SegmentSeparator | WhiteSpace | OtherNeutral,

        /// <summary>
        /// U+202A: the LR embedding control.
        /// </summary>
        [EnumValue("LRE", "U+202A: the LR embedding control")]
        LeftToRightEmbedding = 0b0000_0000_0000_0000_0100_0000_0000_0000,

        /// <summary>
        /// U+202D: the LR override control.
        /// </summary>
        [EnumValue("LRO", "U+202D: the LR override control")]
        LeftToRightOverride = 0b0000_0000_0000_0000_1000_0000_0000_0000,

        /// <summary>
        /// U+202B: the RL embedding control.
        /// </summary>
        [EnumValue("RLE", "U+202B: the RL embedding control")]
        RightToLeftEmbedding = 0b0000_0000_0000_0001_0000_0000_0000_0000,

        /// <summary>
        /// U+202E: the RL override control.
        /// </summary>
        [EnumValue("RLO", "U+202E: the RL override control")]
        RightToLeftOverride = 0b0000_0000_0000_0010_0000_0000_0000_0000,

        /// <summary>
        /// U+202C: terminates an embedding or override control.
        /// </summary>
        [EnumValue("PDF", "U+202C: terminates an embedding or override control")]
        PopDirectionalFormat = 0b0000_0000_0000_0100_0000_0000_0000_0000,

        /// <summary>
        /// U+2066: the LR isolate control.
        /// </summary>
        [EnumValue("LRI", "U+2066: the LR isolate control")]
        LeftToRightIsolate = 0b0000_0000_0000_1000_0000_0000_0000_0000,

        /// <summary>
        /// U+2067: the RL isolate control.
        /// </summary>
        [EnumValue("RLI", "U+2067: the RL isolate control")]
        RightToLeftIsolate = 0b0000_0000_0001_0000_0000_0000_0000_0000,

        /// <summary>
        /// U+2068: the first strong isolate control.
        /// </summary>
        [EnumValue("FSI", "U+2068: the first strong isolate control")]
        FirstStrongIsolate = 0b0000_0000_0010_0000_0000_0000_0000_0000,

        /// <summary>
        /// U+2069: terminates an isolate control.
        /// </summary>
        [EnumValue("PDI", "U+2069: terminates an isolate control")]
        PopDirectionalIsolate = 0b0000_0000_0100_0000_0000_0000_0000_0000,

        /// <summary>
        /// Any explicit formatting character.
        /// </summary>
        [EnumValue("LRE,LRO,RLE,RLO,PDF,LRI,RLI,FSI,PDI", "Any explicit formatting character")]
        ExplicitFormattingTypes = LeftToRightEmbedding | LeftToRightOverride | LeftToRightEmbedding | RightToLeftOverride | PopDirectionalFormat | LeftToRightIsolate | RightToLeftIsolate | FirstStrongIsolate | PopDirectionalIsolate
    }

    /// <summary>
    /// Represents a unicode decomposition type.
    /// </summary>
    public enum UnicodeDecompositionType : byte
    {
        /// <summary>
        /// Unspecified decomposition type.
        /// </summary>
        [EnumValue("", "Unspecified decomposition type")]
        Unspecified = 0,

        /// <summary>
        /// Font variant (for example, a blackletter form).
        /// </summary>
        [EnumValue("<font>", "Font variant (for example, a blackletter form)")]
        Font = 1,

        /// <summary>
        /// No-break version of a space or hyphen.
        /// </summary>
        [EnumValue("<noBreak>", "No-break version of a space or hyphen")]
        NoBreak = 2,

        /// <summary>
        /// Initial presentation form (Arabic).
        /// </summary>
        [EnumValue("<initial>", "Initial presentation form (Arabic)")]
        Initial = 3,

        /// <summary>
        /// Medial presentation form (Arabic).
        /// </summary>
        [EnumValue("<medial>", "Medial presentation form (Arabic)")]
        Medial = 4,

        /// <summary>
        /// Final presentation form (Arabic).
        /// </summary>
        [EnumValue("<final>", "Final presentation form (Arabic)")]
        Final = 5,

        /// <summary>
        /// Isolated presentation form (Arabic).
        /// </summary>
        [EnumValue("<isolated>", "Isolated presentation form (Arabic)")]
        Isolated = 6,

        /// <summary>
        /// Encircled form.
        /// </summary>
        [EnumValue("<circle>", "Encircled form")]
        Encircled = 7,

        /// <summary>
        /// Superscript form.
        /// </summary>
        [EnumValue("<super>", "Superscript form")]
        Superscript = 8,

        /// <summary>
        /// Subscript form.
        /// </summary>
        [EnumValue("<sub>", "Subscript form")]
        Subscript = 9,

        /// <summary>
        /// Vertical layout presentation form.
        /// </summary>
        [EnumValue("<vertical>", "Vertical layout presentation form")]
        Vertical = 10,

        /// <summary>
        /// Wide (or zenkaku) compatibility character.
        /// </summary>
        [EnumValue("<wide>", "Wide (or zenkaku) compatibility character")]
        Wide = 11,

        /// <summary>
        /// Narrow (or hankaku) compatibility character.
        /// </summary>
        [EnumValue("<narrow>", "Narrow (or hankaku) compatibility character")]
        Narrow = 12,

        /// <summary>
        /// Small variant form (CNS compatibility).
        /// </summary>
        [EnumValue("<small>", "Small variant form (CNS compatibility)")]
        Small = 13,

        /// <summary>
        /// CJK squared font variant.
        /// </summary>
        [EnumValue("<square>", "CJK squared font variant")]
        Square = 14,

        /// <summary>
        /// Vulgar fraction form.
        /// </summary>
        [EnumValue("<fraction>", "Vulgar fraction form")]
        Fraction = 15,

        /// <summary>
        /// Otherwise unspecified compatibility character.
        /// </summary>
        [EnumValue("<compat>", "Otherwise unspecified compatibility character")]
        Compat = 16
    }

    /// <summary>
    /// Represents information about codepoint's decomposition data.
    /// </summary>
    public struct UnicodeDecomposition
    {
        /// <summary>
        /// Gets the type of this decomposition.
        /// </summary>
        public UnicodeDecompositionType Type { get; }

        /// <summary>
        /// Gets the codepoints to which this decomposition occurs.
        /// </summary>
        public IEnumerable<UnicodeCodepoint> Codepoints { get { return this.PrivateCodepoints.Select(xs => UnicodeCodepoint.Codepoints[xs]); } }

        /// <summary>
        /// Gets the identifiers of the 
        /// </summary>
        private ImmutableArray<string> PrivateCodepoints { get; }

        /// <summary>
        /// Creates new decomposition information.
        /// </summary>
        /// <param name="type">Type of this decomposition.</param>
        /// <param name="codepoints">Codepoints to which this decomposition occurs.</param>
        public UnicodeDecomposition(UnicodeDecompositionType type, IEnumerable<string> codepoints)
        {
            this.Type = type;
            var cpsb = ImmutableArray.CreateBuilder<string>();
            cpsb.AddRange(codepoints);
            this.PrivateCodepoints = cpsb.ToImmutable();
        }
    }

    /// <summary>
    /// Represents information about codepoint's numeric value.
    /// </summary>
    public struct UnicodeNumericValue
    {
        /// <summary>
        /// Gets the decimal value, if applicable.
        /// </summary>
        public int? Decimal { get; }

        /// <summary>
        /// Gets the digit value, if applicable.
        /// </summary>
        public int? Digit { get; }

        /// <summary>
        /// Gets the numeric value, if applicable.
        /// </summary>
        public string Numeric { get; }

        /// <summary>
        /// Creates information about codepoint's numeric value.
        /// </summary>
        /// <param name="decimal">Decimal value, if applicable.</param>
        /// <param name="digit">Digit value, if applicable.</param>
        /// <param name="numeric">Numeric value, if applicable.</param>
        public UnicodeNumericValue(int? @decimal, int? digit, string numeric)
        {
            this.Decimal = @decimal;
            this.Digit = digit;
            this.Numeric = numeric;
        }
    }

    /// <summary>
    /// Represents information about unicode character block.
    /// </summary>
    public struct UnicodeBlock
    {
        /// <summary>
        /// Gets the collection of unicode blocks.
        /// </summary>
        public static ImmutableList<UnicodeBlock> Blocks { get; private set; }

        /// <summary>
        /// Gets the start of this character block.
        /// </summary>
        public uint Start { get; }

        /// <summary>
        /// Gets the end of this character block.
        /// </summary>
        public uint End { get; }

        /// <summary>
        /// Gets the name 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Constructs information about a unicode character block.
        /// </summary>
        /// <param name="start">Start of this character block.</param>
        /// <param name="end">End of this character block.</param>
        /// <param name="name">Name of this character block.</param>
        public UnicodeBlock(uint start, uint end, string name)
        {
            this.Start = start;
            this.End = end;
            this.Name = name;
        }

        /// <summary>
        /// Loads new block information.
        /// </summary>
        /// <param name="blocks">Block information to load.</param>
        public static void SetBlocks(ImmutableList<UnicodeBlock> blocks)
        {
            Blocks = blocks;
        }

        /// <summary>
        /// Gets the block to which the supplied codepoint belongs.
        /// </summary>
        /// <param name="codepoint">Codepoint to get the block for.</param>
        /// <returns>Requested character block.</returns>
        public static UnicodeBlock GetBlockFor(UnicodeCodepoint codepoint)
        {
            return GetBlockFor(codepoint.CodepointValue);
        }

        /// <summary>
        /// Gets the block to which the supplied codepoint belongs.
        /// </summary>
        /// <param name="codepoint">Codepoint to get the block for.</param>
        /// <returns>Requested character block.</returns>
        public static UnicodeBlock GetBlockFor(string codepoint)
        {
            if (uint.TryParse(codepoint, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                return GetBlockFor(cp);

            throw new ArgumentException("Invalid codepoint supplied.", nameof(codepoint));
        }

        /// <summary>
        /// Gets the block to which the supplied codepoint belongs.
        /// </summary>
        /// <param name="codepoint">Codepoint to get the block for.</param>
        /// <returns>Requested character block.</returns>
        public static UnicodeBlock GetBlockFor(uint codepoint)
        {
            return Blocks.FirstOrDefault(xb => xb.Start <= codepoint && xb.End >= codepoint);
        }
    }

    /// <summary>
    /// Represents informatio about a single unihan codepoint.
    /// </summary>
    public struct UnihanData
    {
        /// <summary>
        /// Gets whether this character is a unihan character.
        /// </summary>
        public bool IsUnihan { get; }

        /// <summary>
        /// Gets the cantonese pronounciation for this character using the jyutping romanization.
        /// </summary>
        public string Cantonese { get; private set; }

        /// <summary>
        /// Gets an English definition for this character. Definitions are for modern written Chinese and are usually (but not always) the same as the definition in other Chinese 
        /// dialects or non-Chinese languages. In some cases, synonyms are indicated. Fuller variant information can be found using the various variant fields.
        /// </summary>
        public string Definition { get; private set; }

        /// <summary>
        /// Gets the modern Korean pronunciation(s) for this character in Hangul.
        /// </summary>
        public string Hangul { get; private set; }

        /// <summary>
        /// Gets the pronunciations and frequencies of this character, based in part on those appearing in Xiandai Hanyu Pinlu Cidian.
        /// </summary>
        public string HanyuPinlu { get; private set; }

        /// <summary>
        /// Gets the pronunciations and frequencies of this character, based in part on those appearing in HDZ.
        /// </summary>
        public string HanyuPinyin { get; private set; }

        /// <summary>
        /// Gets the Japanese pronunciation(s) of this character.
        /// </summary>
        public string JapaneseKun { get; private set; }

        /// <summary>
        /// Gets the Sino-Japanese pronunciation(s) of this character.
        /// </summary>
        public string JapaneseOn { get; private set; }

        /// <summary>
        /// Gets the Korean pronunciation(s) of this character, using the Yale romanization system.
        /// </summary>
        public string Korean { get; private set; }

        /// <summary>
        /// Gets the most customary pinyin reading for this character. When there are two values, then the first is preferred for zh-Hans (CN) and the second is preferred for 
        /// zh-Hant (TW). When there is only one value, it is appropriate for both.
        /// </summary>
        public string Mandarin { get; private set; }

        /// <summary>
        /// Gets the Tang dynasty pronunciation(s) of this character, derived from or consistent with T’ang Poetic Vocabulary by Hugh M. Stimson, Far Eastern Publications, Yale 
        /// Univ. 1976. An asterisk indicates that the word or morpheme represented in toto or in part by the given character with the given reading occurs more than four times 
        /// in the seven hundred poems covered.
        /// </summary>
        public string Tang { get; private set; }

        /// <summary>
        /// Gets the character’s pronunciation(s) in Quốc ngữ.
        /// </summary>
        public string Vietnamese { get; private set; }

        /// <summary>
        /// Gets one or more Hànyǔ Pīnyīn readings as given in the Xiàndài Hànyǔ Cídiǎn.
        /// </summary>
        public string Xhc1983 { get; private set; }

        /// <summary>
        /// Constructs information about this unihan character.
        /// </summary>
        /// <param name="definition">Unihan definition for this character.</param>
        public UnihanData(string definition, string cantonese, string hangul, string hanyu_pinlu, string hanyu_pinyin, string japanese_kun, string japanese_on, string korean, string mandarin,
            string tang, string vietnamese, string xhc1983)
        {
            this.IsUnihan = true;
            this.Definition = definition;
            this.Cantonese = cantonese;
            this.Hangul = hangul;
            this.HanyuPinlu = hanyu_pinlu;
            this.HanyuPinyin = hanyu_pinyin;
            this.JapaneseKun = japanese_kun;
            this.JapaneseOn = japanese_on;
            this.Korean = korean;
            this.Mandarin = mandarin;
            this.Tang = tang;
            this.Vietnamese = vietnamese;
            this.Xhc1983 = xhc1983;
        }
    }

    /// <summary>
    /// Represents information about a single unicode codepoint.
    /// </summary>
    public struct UnicodeCodepoint
    {
        /// <summary>
        /// Gets the complete collection of unicode codepoints.
        /// </summary>
        public static ImmutableDictionary<string, UnicodeCodepoint> Codepoints { get; private set; }

        /// <summary>
        /// Gets the UTF32 encoding used to convert characters.
        /// </summary>
        private static UTF32Encoding UTF32 { get; } = new UTF32Encoding(!BitConverter.IsLittleEndian, false, false);

        /// <summary>
        /// Gets this codepoint.
        /// </summary>
        public string Codepoint { get; }

        /// <summary>
        /// Gets the value of this codepoint.
        /// </summary>
        public uint CodepointValue { get; }

        /// <summary>
        /// Gets the string representing this codepoint.
        /// </summary>
        public string CodepointString { get; }

        /// <summary>
        /// Gets the name of this codepoint.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the category of this codepoint.
        /// </summary>
        public UnicodeCategory Category { get; }

        /// <summary>
        /// Gets the combining class of this codepoint.
        /// </summary>
        public UnicodeCombiningClass CombiningClass { get; }

        /// <summary>
        /// Gets the bidirectionality class of this codepoint.
        /// </summary>
        public UnicodeBidirectionalityClass BidirectionalClass { get; }

        /// <summary>
        /// Gets the decomposition defined for this codepoint.
        /// </summary>
        public UnicodeDecomposition Decomposition { get; }

        /// <summary>
        /// Gets the numeric value defined for this codepoint, if applicable.
        /// </summary>
        public UnicodeNumericValue? NumericValue { get; }

        /// <summary>
        /// Gets whether this character is mirrored in bidirectional text.
        /// </summary>
        public bool BidirectionalMirrored { get; }

        /// <summary>
        /// Gets the name of this codepoint as published in Unicode 1.0.
        /// </summary>
        public string OldUnicodeName { get; }

        /// <summary>
        /// Gets the codepoints that corresponds to the uppercase version of this codepoint.
        /// </summary>
        public UnicodeCodepoint SimpleUppercaseMapping { get { return this.PrivateSimpleUppercaseMapping != null ? Codepoints[this.PrivateSimpleUppercaseMapping] : default; } }

        /// <summary>
        /// Defines the ID of the codepoint that corresponds to uppercase version of this codepoint.
        /// </summary>
        private string PrivateSimpleUppercaseMapping { get; }

        /// <summary>
        /// Gets the codepoints that corresponds to the uppercase version of this codepoint.
        /// </summary>
        public UnicodeCodepoint SimpleLowercaseMapping { get { return this.PrivateSimpleLowercaseMapping != null ? Codepoints[this.PrivateSimpleLowercaseMapping] : default; } }

        /// <summary>
        /// Defines the ID of the codepoint that corresponds to uppercase version of this codepoint.
        /// </summary>
        private string PrivateSimpleLowercaseMapping { get; }

        /// <summary>
        /// Gets the codepoints that corresponds to the uppercase version of this codepoint.
        /// </summary>
        public UnicodeCodepoint SimpleTitlecaseMapping { get { return (this.PrivateSimpleTitlecaseMapping ?? this.PrivateSimpleUppercaseMapping) != null ? Codepoints[this.PrivateSimpleTitlecaseMapping ?? this.PrivateSimpleUppercaseMapping] : default; } }

        /// <summary>
        /// Defines the ID of the codepoint that corresponds to uppercase version of this codepoint.
        /// </summary>
        private string PrivateSimpleTitlecaseMapping { get; }

        /// <summary>
        /// Gets the Unicode block to which this codepoint belongs.
        /// </summary>
        public UnicodeBlock Block { get { return UnicodeBlock.GetBlockFor(this); } }

        /// <summary>
        /// Gets the name of the Unicode block to which this codepoint belongs.
        /// </summary>
        private string PrivateBlock { get; }

        /// <summary>
        /// Gets unihan data associated with this character.
        /// </summary>
        public UnihanData UnihanData { get; }

        /// <summary>
        /// Constructs information about a unicode codepoint.
        /// </summary>
        /// <param name="codepoint">Codepoint to which this object refers.</param>
        /// <param name="name">Name of this codepoint.</param>
        /// <param name="category">Category of this codepoint.</param>
        /// <param name="combining_class">Combining class of this codepoint.</param>
        /// <param name="bidi_class">Bidirectionality class of this codepoint.</param>
        /// <param name="decomposition">Decomposition data of this codepoint.</param>
        /// <param name="numeric_value">Numeric value of this codepoint, if applicable.</param>
        /// <param name="bidi_mirrored">Whether this codepoint is mirrored in bidirectional text.</param>
        /// <param name="old_name">Unicode 1.0 name of this codepoint.</param>
        /// <param name="uppermap">Uppercase mapping for this codepoint.</param>
        /// <param name="lowermap">Lowercase mapping for this codepoint.</param>
        /// <param name="titlemap">Titlecase mapping for this codepoint.</param>
        /// <param name="block_name">Name of unicode block to which this codepoint belongs.</param>
        /// <param name="unihan">Unihan data associated with this unicode codepoint.</param>
        public UnicodeCodepoint(string codepoint, string name, UnicodeCategory category, UnicodeCombiningClass combining_class, UnicodeBidirectionalityClass bidi_class, UnicodeDecomposition decomposition,
            UnicodeNumericValue? numeric_value, bool bidi_mirrored, string old_name, string uppermap, string lowermap, string titlemap, string block_name, UnihanData unihan)
        {
            this.Codepoint = codepoint;
            this.CodepointValue = uint.Parse(codepoint, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            this.CodepointString = UTF32.GetString(BitConverter.GetBytes(this.CodepointValue));
            this.Name = name;
            this.Category = category;
            this.CombiningClass = combining_class;
            this.BidirectionalClass = bidi_class;
            this.Decomposition = decomposition;
            this.NumericValue = numeric_value;
            this.BidirectionalMirrored = bidi_mirrored;
            this.OldUnicodeName = old_name;
            this.PrivateSimpleUppercaseMapping = !string.IsNullOrWhiteSpace(uppermap) ? uppermap : null;
            this.PrivateSimpleLowercaseMapping = !string.IsNullOrWhiteSpace(lowermap) ? lowermap : null;
            this.PrivateSimpleTitlecaseMapping = !string.IsNullOrWhiteSpace(titlemap) ? titlemap : null;
            this.PrivateBlock = block_name;
            this.UnihanData = unihan;
        }

        /// <summary>
        /// Loads new codepoint information.
        /// </summary>
        /// <param name="codepoints">Codepoint information to load.</param>
        public static void SetCodepoints(ImmutableDictionary<string, UnicodeCodepoint> codepoints)
        {
            Codepoints = codepoints;
        }

        /// <summary>
        /// Gets a codepoint by its value.
        /// </summary>
        /// <param name="codepoint">Value of the codepoint to get.</param>
        /// <returns>Requested codepoint.</returns>
        public static UnicodeCodepoint GetCodepoint(uint codepoint)
        {
            return GetCodepoint(codepoint.ToString("X4"));
        }

        /// <summary>
        /// Gets a codepoint by its representation.
        /// </summary>
        /// <param name="codepoint">Representation of codepoint to get.</param>
        /// <returns>Requested codepoint.</returns>
        public static UnicodeCodepoint GetCodepoint(string codepoint)
        {
            codepoint = codepoint.ToUpperInvariant();

            if (Codepoints.TryGetValue(codepoint, out var cp))
                return cp;

            var blk = UnicodeBlock.GetBlockFor(codepoint);
            return new UnicodeCodepoint(codepoint, string.Concat(blk.Name.ToUpperInvariant(), " - ", codepoint), UnicodeCategory.Unknown, UnicodeCombiningClass.NotReordered,
                UnicodeBidirectionalityClass.Unknown, new UnicodeDecomposition(UnicodeDecompositionType.Unspecified, new string[0]), null, false, null, null, null, null, blk.Name, default);
        }
    }

    /// <summary>
    /// Helper class, which loads unicode data from external sources.
    /// </summary>
    public class UnicodeDataLoader : IDisposable
    {
        /// <summary>
        /// Gets the raw stream with the data.
        /// </summary>
        private FileStream SourceFileStream { get; }

        /// <summary>
        /// Gets the stream containing decompressed data.
        /// </summary>
        private GZipStream SourceDecompressedStream { get; }

        /// <summary>
        /// Gets the mapping of string values to categories.
        /// </summary>
        private ImmutableDictionary<string, UnicodeCategory> CategoryMap { get; }

        /// <summary>
        /// Gets the mapping of string values to combining classes.
        /// </summary>
        private ImmutableDictionary<string, UnicodeCombiningClass> CombiningClassMap { get; }

        /// <summary>
        /// Gets the mapping of string values to bidirectionality classes.
        /// </summary>
        private ImmutableDictionary<string, UnicodeBidirectionalityClass> BidirectionalityClassMap { get; }

        /// <summary>
        /// Gets the mapping of string values to decomposition types.
        /// </summary>
        private ImmutableDictionary<string, UnicodeDecompositionType> DecompositionTypeMap { get; }

        /// <summary>
        /// Creates a new unicode data loader from specified file.
        /// </summary>
        /// <param name="filename">File with unicode data to load.</param>
        public UnicodeDataLoader(string filename)
        {
            var fi = new FileInfo(filename);
            this.SourceFileStream = fi.OpenRead();
            this.SourceDecompressedStream = new GZipStream(this.SourceFileStream, CompressionMode.Decompress, true);

            this.CategoryMap = UnicodeExtensions.BuildCategoryMap();
            this.CombiningClassMap = UnicodeExtensions.BuildCombiningClassMap();
            this.BidirectionalityClassMap = UnicodeExtensions.BuildBidirectionalityClassMap();
            this.DecompositionTypeMap = UnicodeExtensions.BuildDecompositionTypeMap();
        }

        /// <summary>
        /// Asynchronously loads the codepoint data from specified file.
        /// </summary>
        /// <returns></returns>
        public async Task LoadCodepointsAsync()
        {
            byte[] buff = null;
            using (var ms = new MemoryStream())
            {
                await this.SourceDecompressedStream.CopyToAsync(ms).ConfigureAwait(false);
                buff = ms.ToArray();
            }

            var utf8 = new UTF8Encoding(false);
            var json = utf8.GetString(buff);

            var rawdat = JsonConvert.DeserializeObject<RawUnicodeData>(json);

            var rawblk = rawdat.Blocks;
            var blks = ImmutableList.CreateBuilder<UnicodeBlock>();
            foreach (var xrblk in rawblk)
                blks.Add(new UnicodeBlock(xrblk.Start, xrblk.End, xrblk.Name));

            UnicodeBlock.SetBlocks(blks.ToImmutable());

            // TODO: Unihan
            var rawhan = rawdat.Unihan;
            var hans = new Dictionary<string, UnihanData>();
            foreach (var xhan in rawhan)
                hans[xhan.Codepoint] = new UnihanData(xhan.Definition, xhan.Cantonese, xhan.Hangul, xhan.HanyuPinlu, xhan.HanyuPinyin, xhan.JapaneseKun, xhan.JapaneseOn, xhan.Korean, xhan.Mandarin,
                    xhan.Tang, xhan.Vietnamese, xhan.Xhc1983);

            var rawcps = rawdat.Characters;
            var cps = ImmutableDictionary.CreateBuilder<string, UnicodeCodepoint>();
            foreach (var xrcp in rawcps)
            {
                var cat = this.CategoryMap[xrcp.GeneralCategory];
                var cmbc = this.CombiningClassMap[xrcp.CanonicalCombiningClass];
                var bidic = this.BidirectionalityClassMap[xrcp.BidiClass];

                var unidec = new UnicodeDecomposition(UnicodeDecompositionType.Unspecified, new string[0]);
                if (!string.IsNullOrWhiteSpace(xrcp.DecompositionTypeAndMapping))
                {
                    var decdat = xrcp.DecompositionTypeAndMapping.Split(' ');
                    var dect = UnicodeDecompositionType.Unspecified;
                    if (decdat[0].StartsWith("<"))
                        dect = this.DecompositionTypeMap[decdat[0]];

                    unidec = new UnicodeDecomposition(dect, decdat.Skip(1));
                }

                UnicodeNumericValue? numval = null;
                if (!string.IsNullOrWhiteSpace(xrcp.NumericValueDecimal) || !string.IsNullOrWhiteSpace(xrcp.NumericValueDigit) || !string.IsNullOrWhiteSpace(xrcp.NumericValueNumeric))
                {
                    int? dec = null;
                    if (!string.IsNullOrWhiteSpace(xrcp.NumericValueDecimal))
                        dec = int.Parse(xrcp.NumericValueDecimal);

                    int? dig = null;
                    if (!string.IsNullOrWhiteSpace(xrcp.NumericValueDigit))
                        dec = int.Parse(xrcp.NumericValueDigit);

                    string num = null;
                    if (!string.IsNullOrWhiteSpace(xrcp.NumericValueNumeric))
                        num = xrcp.NumericValueNumeric;

                    numval = new UnicodeNumericValue(dec, dig, num);
                }

                if (!hans.TryGetValue(xrcp.Codepoint, out var unihan))
                    unihan = default;

                cps[xrcp.Codepoint] = new UnicodeCodepoint(xrcp.Codepoint, xrcp.Name, cat, cmbc, bidic, unidec, numval, xrcp.BidiMirrored.ToLowerInvariant() == "Y", xrcp.OldUnicodeName,
                    xrcp.SimpleUppercaseMapping.Trim(), xrcp.SimpleLowercaseMapping.Trim(), xrcp.SimpleTitlecaseMapping.Trim(), xrcp.BlockName, unihan);
            }

            var missing = hans.Keys.Except(cps.Keys);
            foreach (var xm in missing)
            {
                var blk = UnicodeBlock.GetBlockFor(xm);
                cps[xm] = new UnicodeCodepoint(xm, string.Concat(blk.Name.ToUpperInvariant(), " - ", xm), UnicodeCategory.Unknown, UnicodeCombiningClass.NotReordered, UnicodeBidirectionalityClass.Unknown,
                    new UnicodeDecomposition(UnicodeDecompositionType.Unspecified, new string[0]), null, false, null, null, null, null, blk.Name, hans[xm]);
            }

            UnicodeCodepoint.SetCodepoints(cps.ToImmutable());
        }

        /// <summary>
        /// Disposes of this data loader.
        /// </summary>
        public void Dispose()
        {
            this.SourceDecompressedStream.Dispose();
            this.SourceFileStream.Dispose();
        }
    }

    /// <summary>
    /// Various extensions to the unicode classes.
    /// </summary>
    public static class UnicodeExtensions
    {
        /// <summary>
        /// Gets the mapping of categories to string values.
        /// </summary>
        private static ImmutableDictionary<UnicodeCategory, EnumValueAttribute> CategoryMap { get; }

        /// <summary>
        /// Gets the mapping of combining classes to string values.
        /// </summary>
        private static ImmutableDictionary<UnicodeCombiningClass, EnumValueAttribute> CombiningClassMap { get; }

        /// <summary>
        /// Gets the mapping of bidirectionality classes to string values.
        /// </summary>
        private static ImmutableDictionary<UnicodeBidirectionalityClass, EnumValueAttribute> BidirectionalityClassMap { get; }

        /// <summary>
        /// Gets the mapping of decomposition types to string values.
        /// </summary>
        private static ImmutableDictionary<UnicodeDecompositionType, EnumValueAttribute> DecompositionTypeMap { get; }

        /// <summary>
        /// Gets the UTF-32 encoding used to decompose strings into codepoints.
        /// </summary>
        private static UTF32Encoding UTF32 { get; }

        static UnicodeExtensions()
        {
            var t1 = typeof(UnicodeCategory);
            var ti1 = t1.GetTypeInfo();
            var fs1 = ti1.GetFields(BindingFlags.Public | BindingFlags.Static);
            var d1 = ImmutableDictionary.CreateBuilder<UnicodeCategory, EnumValueAttribute>();
            foreach (var xf in fs1)
            {
                var xa = xf.GetCustomAttribute<EnumValueAttribute>();
                if (xa == null)
                    continue;

                var xv = (UnicodeCategory)xf.GetValue(null);
                d1[xv] = xa;
            }
            CategoryMap = d1.ToImmutable();

            var t2 = typeof(UnicodeCombiningClass);
            var ti2 = t2.GetTypeInfo();
            var fs2 = ti2.GetFields(BindingFlags.Public | BindingFlags.Static);
            var d2 = ImmutableDictionary.CreateBuilder<UnicodeCombiningClass, EnumValueAttribute>();
            foreach (var xf in fs2)
            {
                var xa = xf.GetCustomAttribute<EnumValueAttribute>();
                if (xa == null)
                    continue;

                var xv = (UnicodeCombiningClass)xf.GetValue(null);
                d2[xv] = xa;
            }
            CombiningClassMap = d2.ToImmutable();

            var t3 = typeof(UnicodeBidirectionalityClass);
            var ti3 = t3.GetTypeInfo();
            var fs3 = ti3.GetFields(BindingFlags.Public | BindingFlags.Static);
            var d3 = ImmutableDictionary.CreateBuilder<UnicodeBidirectionalityClass, EnumValueAttribute>();
            foreach (var xf in fs3)
            {
                var xa = xf.GetCustomAttribute<EnumValueAttribute>();
                if (xa == null)
                    continue;

                var xv = (UnicodeBidirectionalityClass)xf.GetValue(null);
                d3[xv] = xa;
            }
            BidirectionalityClassMap = d3.ToImmutable();

            var t4 = typeof(UnicodeDecompositionType);
            var ti4 = t4.GetTypeInfo();
            var fs4 = ti4.GetFields(BindingFlags.Public | BindingFlags.Static);
            var d4 = ImmutableDictionary.CreateBuilder<UnicodeDecompositionType, EnumValueAttribute>();
            foreach (var xf in fs4)
            {
                var xa = xf.GetCustomAttribute<EnumValueAttribute>();
                if (xa == null)
                    continue;

                var xv = (UnicodeDecompositionType)xf.GetValue(null);
                d4[xv] = xa;
            }
            DecompositionTypeMap = d4.ToImmutable();

            UTF32 = new UTF32Encoding(false, false, true);
        }

        /// <summary>
        /// Converts this <see cref="UnicodeCategory"/> to a user-friendly string.
        /// </summary>
        /// <param name="cat"><see cref="UnicodeCategory"/> to convert.</param>
        /// <returns>User-friendly string representing this value.</returns>
        public static string ToDescription(this UnicodeCategory cat)
        {
            return CategoryMap.ContainsKey(cat) ? CategoryMap[cat].Description : cat.ToString();
        }

        /// <summary>
        /// Retrieves the value of this <see cref="UnicodeCategory"/>.
        /// </summary>
        /// <param name="cat"><see cref="UnicodeCategory"/> to retrieve the value of.</param>
        /// <returns>String value of this enum.</returns>
        public static string ToValue(this UnicodeCategory cat)
        {
            return CategoryMap.ContainsKey(cat) ? CategoryMap[cat].Value : cat.ToString();
        }

        /// <summary>
        /// Converts this <see cref="UnicodeCombiningClass"/> to a user-friendly string.
        /// </summary>
        /// <param name="ucc"><see cref="UnicodeCombiningClass"/> to convert.</param>
        /// <returns>User-friendly string representing this value.</returns>
        public static string ToDescription(this UnicodeCombiningClass ucc)
        {
            return CombiningClassMap.ContainsKey(ucc) ? CombiningClassMap[ucc].Description : ucc.ToString();
        }

        /// <summary>
        /// Retrieves the value of this <see cref="UnicodeCombiningClass"/>.
        /// </summary>
        /// <param name="ucc"><see cref="UnicodeCombiningClass"/> to retrieve the value of.</param>
        /// <returns>String value of this enum.</returns>
        public static string ToValue(this UnicodeCombiningClass ucc)
        {
            return CombiningClassMap.ContainsKey(ucc) ? CombiningClassMap[ucc].Value : ucc.ToString();
        }

        /// <summary>
        /// Converts this <see cref="UnicodeBidirectionalityClass"/> to a user-friendly string.
        /// </summary>
        /// <param name="ubc"><see cref="UnicodeBidirectionalityClass"/> to convert.</param>
        /// <returns>User-friendly string representing this value.</returns>
        public static string ToDescription(this UnicodeBidirectionalityClass ubc)
        {
            return BidirectionalityClassMap.ContainsKey(ubc) ? BidirectionalityClassMap[ubc].Description : ubc.ToString();
        }

        /// <summary>
        /// Retrieves the value of this <see cref="UnicodeBidirectionalityClass"/>.
        /// </summary>
        /// <param name="ubc"><see cref="UnicodeBidirectionalityClass"/> to retrieve the value of.</param>
        /// <returns>String value of this enum.</returns>
        public static string ToValue(this UnicodeBidirectionalityClass ubc)
        {
            return BidirectionalityClassMap.ContainsKey(ubc) ? BidirectionalityClassMap[ubc].Description : ubc.ToString();
        }

        /// <summary>
        /// Converts this <see cref="UnicodeDecompositionType"/> to a user-friendly string.
        /// </summary>
        /// <param name="udt"><see cref="UnicodeDecompositionType"/> to convert.</param>
        /// <returns>User-friendly string representing this value.</returns>
        public static string ToDescription(this UnicodeDecompositionType udt)
        {
            return DecompositionTypeMap.ContainsKey(udt) ? DecompositionTypeMap[udt].Description : udt.ToString();
        }

        /// <summary>
        /// Retrieves the value of this <see cref="UnicodeDecompositionType"/>.
        /// </summary>
        /// <param name="udt"><see cref="UnicodeDecompositionType"/> to retrieve the value of.</param>
        /// <returns>String value of this enum.</returns>
        public static string ToValue(this UnicodeDecompositionType udt)
        {
            return DecompositionTypeMap.ContainsKey(udt) ? DecompositionTypeMap[udt].Description : udt.ToString();
        }

        /// <summary>
        /// Converts this string into a collection of codepoints.
        /// </summary>
        /// <param name="s">String to convert.</param>
        /// <returns>Collection of codepoints this string is comprised of.</returns>
        public static IEnumerable<UnicodeCodepoint> ToCodepoints(this string s)
        {
            var bts = UTF32.GetBytes(s);
            return Enumerable.Range(0, bts.Length / 4)
                .Select(xi => (uint)(bts[xi * 4] | bts[xi * 4 + 1] << 8 | bts[xi * 4 + 2] << 16 | bts[xi * 4 + 3]))
                .Select(xui => UnicodeCodepoint.GetCodepoint(xui));
        }

        public static ImmutableDictionary<string, UnicodeCategory> BuildCategoryMap()
        {
            var db = ImmutableDictionary.CreateBuilder<string, UnicodeCategory>();
            db.AddRange(CategoryMap.Select(xkvp => new KeyValuePair<string, UnicodeCategory>(xkvp.Value.Value, xkvp.Key)));
            return db.ToImmutable();
        }

        public static ImmutableDictionary<string, UnicodeCombiningClass> BuildCombiningClassMap()
        {
            var db = ImmutableDictionary.CreateBuilder<string, UnicodeCombiningClass>();
            db.AddRange(CombiningClassMap.Select(xkvp => new KeyValuePair<string, UnicodeCombiningClass>(xkvp.Value.Value, xkvp.Key)));
            return db.ToImmutable();
        }

        public static ImmutableDictionary<string, UnicodeBidirectionalityClass> BuildBidirectionalityClassMap()
        {
            var db = ImmutableDictionary.CreateBuilder<string, UnicodeBidirectionalityClass>();
            db.AddRange(BidirectionalityClassMap.Select(xkvp => new KeyValuePair<string, UnicodeBidirectionalityClass>(xkvp.Value.Value, xkvp.Key)));
            return db.ToImmutable();
        }

        public static ImmutableDictionary<string, UnicodeDecompositionType> BuildDecompositionTypeMap()
        {
            var db = ImmutableDictionary.CreateBuilder<string, UnicodeDecompositionType>();
            db.AddRange(DecompositionTypeMap.Select(xkvp => new KeyValuePair<string, UnicodeDecompositionType>(xkvp.Value.Value, xkvp.Key)));
            return db.ToImmutable();
        }
    }
}
