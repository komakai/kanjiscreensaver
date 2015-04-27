//
//  Kanji.h
//  Time In Words Screen Saver
//
//  Created by Giles Payne on 4/21/15.
//

#import <Foundation/Foundation.h>

@interface Kanji : NSObject
@property unichar kanji;
@property int level;
@property NSString* meaning;
@property NSMutableArray* keywords;
@end

@interface Keyword : NSObject
@property NSString* word;
@property NSString* kana;
@property NSInteger kanaHighlightStart;
@property NSInteger kanaHighlightLength;
@property NSString* romaji;
@property NSInteger romajiHighlightStart;
@property NSInteger romajiHighlightLength;
@property NSString* meaning;
@end
