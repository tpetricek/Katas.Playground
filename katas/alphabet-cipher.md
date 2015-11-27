<img src="/alphabet-cipher/letter.gif" style="float:right;height:240px;margin:0px 50px 0px 50px" />

Lewis Carroll's Alphabet Cipher
===============================


Lewis Carroll published a cipher known as
[The Alphabet Cipher](http://en.wikipedia.org/wiki/The_Alphabet_Cipher)
in 1868, possibly in a children's magazine. It describes what is known as a 
[Vigenère cipher](https://en.wikipedia.org/wiki/Vigen%C3%A8re_cipher), 
a well-known scheme in cryptography.

In this Kata, you will implement encoding and decoding of text using the
cipher. A bonus problem is to guess the secret phrase when you have original
text and its encoded version.

_Ported from [@gigasquid](https://twitter.com/gigasquid)'s
[Wonderland Clojure Katas](https://github.com/gigasquid/wonderland-clojure-katas)._

********************************************************************************

# Introduction

The Alphabet Cipher: Introduction
---------------------------------

This Alphabet Cipher involves alphabet substitution using a keyword.
First you must make a substitution chart like this, where each row of
the alphabet is rotated by one as each letter goes down the chart.

```
   ABCDEFGHIJKLMNOPQRSTUVWXYZ
 A abcdefghijklmnopqrstuvwxyz
 B bcdefghijklmnopqrstuvwxyza
 C cdefghijklmnopqrstuvwxyzab
 D defghijklmnopqrstuvwxyzabc
 E efghijklmnopqrstuvwxyzabcd
 F fghijklmnopqrstuvwxyzabcde
 G ghijklmnopqrstuvwxyzabcdef
 H hijklmnopqrstuvwxyzabcdefg
 I ijklmnopqrstuvwxyzabcdefgh
 J jklmnopqrstuvwxyzabcdefghi
 K klmnopqrstuvwxyzabcdefghij
 L lmnopqrstuvwxyzabcdefghijk
 M mnopqrstuvwxyzabcdefghijkl
 N nopqrstuvwxyzabcdefghijklm
 O opqrstuvwxyzabcdefghijklmn
 P pqrstuvwxyzabcdefghijklmno
 Q qrstuvwxyzabcdefghijklmnop
 R rstuvwxyzabcdefghijklmnopq
 S stuvwxyzabcdefghijklmnopqr
 T tuvwxyzabcdefghijklmnopqrs
 U uvwxyzabcdefghijklmnopqrst
 V vwxyzabcdefghijklmnopqrstu
 W wxyzabcdefghijklmnopqrstuv
 X xyzabcdefghijklmnopqrstuvw
 Y yzabcdefghijklmnopqrstuvwx
 Z zabcdefghijklmnopqrstuvwxy
```

Both parties need to decide on a secret keyword.  This keyword is not written down anywhere, but memorized.

To encode the message, first write down the message.

```
meetmebythetree
```

Then, write the keyword, (which in this case is _scones_), repeated as many times as necessary.

```
sconessconessco
meetmebythetree
```

Now you can look up the column _S_ in the table and follow it down until it meets the _M_ row. The value at the intersection is the letter _e_.  All the letters would be thus encoded.

```
sconessconessco
meetmebythetree
egsgqwtahuiljgs
```

The encoded message is now `egsgqwtahuiljgs`

To decode, the person would use the secret keyword and do the opposite.

--------------------------------------------------------------------------------

# Playground

The Alphabet Cipher: Playground
-------------------------------

```source
let encode (key:string) (message:string) = 
  "encodeme"
  
let decode (key:string) (message:string) = 
  "decodeme"
  
let decipher (cipher:string) (message:string) = 
  "decypherme" 
```

foo

```test
encode "vigilance" "meetmeontuesdayeveningatseven" = "hmkbxebpxpmyllyrxiiqtoltfgzzv"
```

```test
encode "scones" "meetmebythetree" = "egsgqwtahuiljgs"
```

Verify decoding

```test
decode "vigilance" "hmkbxebpxpmyllyrxiiqtoltfgzzv" = "meetmeontuesdayeveningatseven"
```

```test
decode "scones" "egsgqwtahuiljgs" = "meetmebythetree"
```

verify decyphering

```test
decipher "opkyfipmfmwcvqoklyhxywgeecpvhelzg" "thequickbrownfoxjumpsoveralazydog" = "vigilance"
```

```test
decipher "hcqxqqtqljmlzhwiivgbsapaiwcenmyu" "packmyboxwithfivedozenliquorjugs" = "scones"
```

--------------------------------------------------------------------------------

# License

The Alphabet Cipher: License
---------------------------------


Copyright (c) 2015 Mathias Brandewinder / MIT License.

Original Clojure version: Copyright © 2014 Carin Meier, distributed under the Eclipse Public License either version 1.0 or (at
your option) any later version.

_Ported from [@gigasquid](https://twitter.com/gigasquid)'s
[*wonderland-clojure-katas*](https://github.com/gigasquid/wonderland-clojure-katas)_

