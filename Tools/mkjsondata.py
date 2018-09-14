#!/usr/bin/env python3
# This file is part of Companion Cube project
#
# Copyright 2018 Emzi0767
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# ------------------------------------------------------------------------
#
# This generates unicode_data.json.gz from source Unicode data.
# This requires UnicodeData.txt and Blocks.txt files from:
# https://unicode.org/Public/10.0.0/ucd/UCD.zip
#
# It also requires Unihan_Readings.txt file from:
# https://unicode.org/Public/10.0.0/ucd/Unihan.zip
#
# You can check for newer versions here:
# https://unicode.org/Public/
#
# Place the required files in the same directory as the script and run it.
# The generated unicode_data.json.gz will be placed in the directory the
# script was ran form.

from typing import List, Dict, Any
from re import compile as re_compile
from json import dumps
from gzip import open as gzopen


class UnicodeBlock:
    def __init__(self, startcp: str, endcp: str, name: str):
        self.__slots__ = ["start_codepoint", "end_codepoint", "start", "end", "name"]

        self.start_codepoint = startcp
        self.end_codepoint = endcp

        self.start = int(startcp, 16)
        self.end = int(endcp, 16)

        self.name = name

    def to_dict(self) -> Dict[str, Any]:
        return {
            "start_codepoint": self.start_codepoint,
            "end_codepoint": self.end_codepoint,
            "start": self.start,
            "end": self.end,
            "name": self.name
        }


class UnicodeCodepoint:
    def __init__(self, udata: List[str], block: UnicodeBlock):
        self.__slots__ = ["codepoint", "value", "name", "general_category", "canonical_combining_class", "bidi_class",
            "decomposition_type_and_mapping", "numeric_value_decimal", "numeric_value_digit", "numeric_value_numeric",
            "bidi_mirrored", "old_unicode_name", "iso_comment", "simple_uppercase_mapping",
            "simple_lowercase_mapping", "simple_titlecase_mapping", "block_name"]

        self.codepoint = udata[0]
        self.value = int(udata[0], 16)
        self.name = udata[1]
        self.general_category = udata[2]
        self.canonical_combining_class = udata[3]
        self.bidi_class = udata[4]
        self.decomposition_type_and_mapping = udata[5]
        self.numeric_value_decimal = udata[6]
        self.numeric_value_digit = udata[7]
        self.numeric_value_numeric = udata[8]
        self.bidi_mirrored = udata[9]
        self.old_unicode_name = udata[10]
        self.iso_comment = udata[11]
        self.simple_uppercase_mapping = udata[12]
        self.simple_lowercase_mapping = udata[13]
        self.simple_titlecase_mapping = udata[14]
        self.block_name = block.name

    def to_dict(self) -> Dict[str, Any]:
        d = {}
        for slot in self.__slots__:
            d[slot] = getattr(self, slot)

        return d

class UnihanData:
    def __init__(self, codepoint: str):
        self.__slots__ = ["codepoint", "cantonese", "definition", "hangul", "hanyu_pinlu", "hanyu_pinyin", 
		"japanese_kun", "japanese_on", "korean", "mandarin", "tang", "vietnamese", "xhc1983"]

        self.codepoint = codepoint

        self.cantonese = None
        self.definition = None
        self.hangul = None
        self.hanyu_pinlu = None
        self.hanyu_pinyin = None
        self.japanese_kun = None
        self.japanese_on = None
        self.korean = None
        self.mandarin = None
        self.tang = None
        self.vietnamese = None
        self.xhc1983 = None

    def to_dict(self) -> Dict[str, Any]:
        d = {}
        for slot in self.__slots__:
            d[slot] = getattr(self, slot)

        return d


def json_callback(x):
    return x.to_dict()


def main():
    blocks = []
    with open("Blocks.txt", "r", encoding="utf-8") as f:
        for line in f:
            if not line.strip() or line[0] == "#":
                continue

            bdata0 = line.split(";")
            bdata0[1] = bdata0[1].strip()
            bdata1 = bdata0[0].split("..")

            blocks.append(UnicodeBlock(bdata1[0], bdata1[1], bdata0[1]))

    han_re = re_compile(r"[A-Z]")
    unihan = {}
    with open("Unihan_Readings.txt", "r", encoding="utf-8") as f:
        for line in f:
            if not line.strip() or line[0] == "#":
                continue

            bdata0 = line.split("\t")
            cp = bdata0[0][2:]
            if cp not in unihan:
                unihan[cp] = UnihanData(cp)

            prop = bdata0[1][1:]
            prop = han_re.sub(lambda m: "_" + m.group(0).lower(), prop)[1:]

            setattr(unihan[cp], prop, bdata0[2].strip())

    data = []
    with open("UnicodeData.txt", "r", encoding="utf-8") as f:
        for line in f:
            if not line.strip() or line[0] == "#":
                contine

            udata = line.split(";")
            uval = int(udata[0], 16)

            for block in blocks:
                if block.start <= uval and block.end >= uval:
                    tblock = block
                    break

            data.append(UnicodeCodepoint(udata, tblock))

    unidata = {"blocks": blocks, "unihan": list(unihan.values()), "characters": data}
    unidata_json = dumps(unidata, default=json_callback)

    with gzopen("unicode_data.json.gz", "wb") as f:
        f.write(unidata_json.encode("utf-8"))


if __name__ == "__main__":
    main()
