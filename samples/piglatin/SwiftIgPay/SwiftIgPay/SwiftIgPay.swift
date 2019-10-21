//
//  SwiftIgPay.swift
//  SwiftIgPay
//
//  Created by Steve Hawley on 10/11/17.
//  Copyright © 2017 Steve Hawley. All rights reserved.
//

import Foundation

private func isSpace(_ c:Character) -> Bool
{
    let spaces = " \t\n\r"
    return spaces.characters.index(of: c) != nil
}

private func wordify(s:String) ->[String]
{
    var words:[String] = []
    var inSpace = true;
    var currWord = "";
    
    for i in s.characters.indices {
        if inSpace {
            if isSpace(s[i]) {
                continue;
            }
            inSpace = false;
            currWord = currWord + String(s[i])
        }
        else {
            if isSpace(s[i]) {
                inSpace = true;
                words.append(currWord)
                currWord = ""
            }
            else {
                currWord = currWord + String(s[i])
            }
        }
    }
    if !inSpace {
        words.append(currWord)
    }
    
    return words
}

private func isVowel(_ c:Character) -> Bool
{
    let vowels = "aeiouAEIOU"
    return vowels.contains(String(c))
}

private func latinfy(word:String) -> String
{
    let start = word.characters.startIndex
    var firstVowel = start
    for i in word.characters.indices {
        if isVowel(word[i]) {
            firstVowel = i
            break;
        }
    }
    if firstVowel == start || firstVowel > word.characters.endIndex {
        return word + "-way"
    }
    else {
        let prefix = word.characters[start ..< firstVowel]
        let stem = word.substring(from: firstVowel)
        return stem + "-" + String(prefix) + "ay"
    }
}

public class IgPay {
    var _words:[String]
    public init(s:String)
    {
        _words = wordify(s:s)
        for i in 0..<_words.count {
            _words[i] = latinfy(word:_words[i])
        }
    }
    
    public var count:Int {
        get { return _words.count }
    }
    
    public subscript(i:Int) -> String {
        get {
            return _words[i]
        }
    }
}
