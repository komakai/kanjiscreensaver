//
//

#import "KanjiScreenSaverView.h"
#import "Kanji.h"

#define kModuleName     @"com.japandiary.Kanji-Screen-Saver"

static const int keywordParts = 5;
static const int maxKeywords = 10;
static const int kanjiFontHeight = 80;
static const int meaningFontHeight = 20;
static const int keywordFontHeight = 14;

@implementation KanjiScreenSaverView {
    NSArray* levels;
    NSString* includeKanjis;
    NSString* excludeKanjis;
    NSString* fontName;
    NSInteger duration;
    NSMutableArray* kanjis;
    NSFont* kanjiFont;
    NSFont* meaningFont;
    NSFont* keywordFontNormal;
    NSFont* keywordFontBold;
    NSTextField* kanjiLabel;
    NSTextField* meaningLabel;
    NSTextField* keywordLabels[keywordParts*maxKeywords];
    BOOL isPreviewMode;
    int fontFactor;
}

- (void)initConfigKanjis
{
    if (kanjis == nil) {
        [self loadConfig];
        [self loadKanji];
        [self initLabels];
    }
}

- (id)initWithFrame:(NSRect)frame isPreview:(BOOL)isPreview
{
    self = [super initWithFrame:frame isPreview:isPreview];
    isPreviewMode = isPreview;
    fontFactor = isPreviewMode?2:1;
    if (self) {
        [self initConfigKanjis];
        [self setAnimationTimeInterval:duration/1000.0];
    }
    return self;
}

- (BOOL)isOpaque {
    return YES;
}

// Standard functions created for you when you create a screen saver project in Xcode
- (void)startAnimation
{
    [super startAnimation];
}

- (void)stopAnimation
{
    [super stopAnimation];
}

- (void)drawRect:(NSRect)rect
{
    [super drawRect:rect];
}

- (void)animateOneFrame
{
    [self initConfigKanjis];
    [self updateKanji];
}

#pragma mark -  Config

- (void)loadConfig
{
    ScreenSaverDefaults *defaults = [ScreenSaverDefaults defaultsForModuleWithName:kModuleName];
    [defaults registerDefaults:@{@"Duration": @5000, @"Levels": @"100,90", @"IncludeKanjis": @"", @"ExcludeKanjis": @"", @"FontName": @"Hiragino Kaku Gothic ProN"}];

    duration = [defaults integerForKey:@"Duration"];
    includeKanjis = [defaults objectForKey:@"IncludeKanjis"];
    excludeKanjis = [defaults objectForKey:@"ExcludeKanjis"];
    fontName = [defaults objectForKey:@"FontName"];
    levels = [[defaults objectForKey:@"Levels"] componentsSeparatedByString:@","];
}

- (BOOL)hasConfigureSheet
{
    return YES;
}

- (NSWindow*)configureSheet
{
    if (!self.configWindow)
    {
        // open from xib file
        if (![[NSBundle bundleForClass:[self class]] loadNibNamed:@"Config" owner:self topLevelObjects:nil]) {
            NSLog( @"Failed to load configure sheet." );
            NSBeep();
        }
    }
    [self loadConfig];

    NSArray *subViews = [[self.configWindow contentView] subviews];
    for (NSView* subView in subViews) {
        if ( [subView isKindOfClass:[NSButton class]]) {
            if ([levels containsObject: [@(subView.tag) stringValue]] ) {
                ((NSButton*)subView).state = YES;
            }
       }
    }
    includeKanjisEdit.stringValue = includeKanjis;
    excludeKanjisEdit.stringValue = excludeKanjis;
    durationSlider.integerValue = duration;
    durationText.stringValue = [NSString stringWithFormat:@"%1.1fs", duration/1000.0f];
    NSArray *fntNames = [[NSFontManager sharedFontManager] availableFontFamilies];
    for (NSString* fntName in fntNames ) {
        if ([fntName characterAtIndex:0] == '.') {
            continue;
        }
        NSFont *font = [NSFont fontWithName:fntName size:14.0];
        if ([[font coveredCharacterSet] characterIsMember:0x5CE0] && [[font coveredCharacterSet] characterIsMember:0x7551] && [[font coveredCharacterSet] characterIsMember:0x99C5] && [[font coveredCharacterSet] characterIsMember:0x3000]) {
            [fontCombo addItemWithTitle:fntName];
        }
    }
    [fontCombo selectItemWithTitle:fontName];
    
    // return a reference to the config window
    return self.configWindow;
}

- (IBAction)okClick:(id)sender {
    // save new settings
    ScreenSaverDefaults *defaults = [ScreenSaverDefaults defaultsForModuleWithName:kModuleName];
    [defaults setObject:@(duration) forKey:@"Duration"];
    [defaults setObject:[includeKanjisEdit stringValue] forKey:@"IncludeKanjis"];
    [defaults setObject:[excludeKanjisEdit stringValue] forKey:@"ExcludeKanjis"];
    [defaults setObject:fontName forKey:@"FontName"];
    NSArray *subViews = [[self.configWindow contentView] subviews];
    NSMutableString *levelsSetting = [NSMutableString string];
    for (NSView* subView in subViews) {
        if ( [subView isKindOfClass:[NSButton class]]) {
            if ( ((NSButton*)subView).state && subView.tag > 0 ) {
                if (levelsSetting.length > 0) {
                    [levelsSetting appendString:@","];
                }
                [levelsSetting appendString:[@(subView.tag) stringValue]];
            }
        }
    }
    [defaults setObject:(NSString*)levelsSetting forKey:@"Levels"];
    [defaults synchronize];

    // close the window
    [[self.configWindow sheetParent] endSheet:self.configWindow];
}

- (IBAction)cancelClick:(id)sender {
    // close the window
    [[self.configWindow sheetParent] endSheet:self.configWindow];
}

- (IBAction)durationChanged:(id)sender {
    duration = durationSlider.integerValue;
    durationText.stringValue = [NSString stringWithFormat:@"%1.1fs", duration/1000.0f];
}

- (IBAction)fontComboChanged:(id)sender {
    fontName = fontCombo.selectedItem.title;
    fontSample.font = [NSFont fontWithName:fontName size:fontSample.font.pointSize];
}

#pragma mark -  Display

- (void)loadKanji
{
    if (levels == nil) {
        [self loadConfig];
    }
    NSString *xmlRes = [[NSBundle bundleForClass:[self class]] pathForResource:@"kanji" ofType:@"xml"];
    NSData *xmlData = [[NSMutableData alloc] initWithContentsOfFile:xmlRes];

    NSXMLDocument *kanjiDoc = [[NSXMLDocument alloc] initWithData:xmlData options:NSXMLNodeOptionsNone error:nil];
    kanjis = [NSMutableArray array];
    
    if (kanjiDoc != nil) {
        NSArray* kanjiNodes = [kanjiDoc nodesForXPath:@"//kanji" error:nil];
        for (int nKanji = 0; nKanji<kanjiNodes.count; nKanji++) {
            NSXMLElement* kanjiNode = [kanjiNodes objectAtIndex:nKanji];
            NSString *level = [kanjiNode attributeForName:@"level"].stringValue;
            NSString *nskanji = [kanjiNode attributeForName:@"char"].stringValue;
            nskanji = [nskanji substringToIndex:1];
            unichar kchar = [nskanji characterAtIndex:0];
            if ( ([levels containsObject:level] || [includeKanjis containsString:nskanji] ) && ![excludeKanjis containsString:nskanji]) {
                Kanji* kanji = [[Kanji alloc] init];
                kanji.kanji = kchar;
                kanji.level = [level intValue];
                kanji.meaning = [kanjiNode attributeForName:@"meaning"].stringValue;
                kanji.keywords = [NSMutableArray array];
                NSArray* keywordNodes = [kanjiNode nodesForXPath:@"keyword" error:nil];
                for (int nKeyword = 0; nKeyword<keywordNodes.count; nKeyword++) {
                    NSXMLElement* keywordNode = [keywordNodes objectAtIndex:nKeyword];
                    Keyword* keyword = [[Keyword alloc] init];
                    keyword.word = [keywordNode attributeForName:@"word"].stringValue;
                    keyword.kana = [keywordNode attributeForName:@"kana"].stringValue;
                    keyword.kanaHighlightStart = [[keywordNode attributeForName:@"kanaHighlightStart"].stringValue integerValue];
                    keyword.kanaHighlightLength = [[keywordNode attributeForName:@"kanaHighlightLength"].stringValue integerValue];
                    if (keyword.kanaHighlightStart == -1 && keyword.kanaHighlightLength == -1) {
                        keyword.kanaHighlightStart = 0;
                        keyword.kanaHighlightLength = [keyword.kana length];
                    }
                    keyword.romaji = [keywordNode attributeForName:@"romaji"].stringValue;
                    keyword.romajiHighlightStart = [[keywordNode attributeForName:@"romajiHighlightStart"].stringValue integerValue];
                    keyword.romajiHighlightLength = [[keywordNode attributeForName:@"romajiHighlightLength"].stringValue integerValue];
                    if (keyword.romajiHighlightStart == -1 && keyword.romajiHighlightLength == -1) {
                        keyword.romajiHighlightStart = 0;
                        keyword.romajiHighlightLength = [keyword.romaji length];
                    }
                    keyword.meaning = [keywordNode attributeForName:@"meaning"].stringValue;
                    [kanji.keywords addObject:keyword];
                }
                [kanjis addObject:kanji];
            }
        }
    }
}

- (NSTextField*)createLabelWithFont:(NSFont*)font {
    NSTextField* label = [[NSTextField alloc] initWithFrame:NSMakeRect(0,0,0,0)];
    [label setFont:font];
    [label setTextColor:[NSColor whiteColor]];
    [label setBezeled:NO];
    [label setDrawsBackground:NO];
    [label setEditable:NO];
    [label setSelectable:NO];
    [[label cell] setWraps:NO];
    [self addSubview:label];
    return label;
}

- (void)initLabels {
    kanjiFont = [NSFont fontWithName:fontName size:kanjiFontHeight/fontFactor];
    meaningFont = [NSFont fontWithName:fontName size:meaningFontHeight/fontFactor];
    keywordFontNormal = [NSFont fontWithName:fontName size:keywordFontHeight/fontFactor];
    keywordFontBold = [[NSFontManager sharedFontManager] fontWithFamily:keywordFontNormal.familyName traits:NSBoldFontMask weight:9.0 size:keywordFontHeight/fontFactor];
    kanjiLabel = [self createLabelWithFont:kanjiFont];
    meaningLabel = [self createLabelWithFont:meaningFont];
    for (int nKeywordIndex=0; nKeywordIndex<maxKeywords; nKeywordIndex++) {
        keywordLabels[keywordParts*nKeywordIndex] = [self createLabelWithFont:keywordFontNormal];
        keywordLabels[keywordParts*nKeywordIndex+1] = [self createLabelWithFont:keywordFontBold];
        keywordLabels[keywordParts*nKeywordIndex+2] = [self createLabelWithFont:keywordFontNormal];
        keywordLabels[keywordParts*nKeywordIndex+3] = [self createLabelWithFont:keywordFontBold];
        keywordLabels[keywordParts*nKeywordIndex+4] = [self createLabelWithFont:keywordFontNormal];
    }
}

- (NSSize)updateLabel:(NSTextField*)label withText:(NSString*)text {
    [label setStringValue:text];
    [label invalidateIntrinsicContentSize];
    return [label intrinsicContentSize];
}

- (void)updateKanji {
    int index = arc4random_uniform((u_int32_t)kanjis.count);
    Kanji* kanji = [kanjis objectAtIndex:index];
    CGFloat width, height;
    NSSize size = [self updateLabel:kanjiLabel withText:[NSString stringWithFormat:@"%C", kanji.kanji]];
    width = size.width;
    height = size.height;
    size = [self updateLabel:meaningLabel withText:kanji.meaning];
    width = MAX(width, size.width);
    height += size.height;
    NSSize keywordSizes[maxKeywords];
    NSInteger keywordIndex = 0;
    for (int i=0; i<maxKeywords*keywordParts; i++) {
        [self updateLabel:keywordLabels[i] withText:@""];
    }
    for (id kw in kanji.keywords) {
        Keyword* keyword = (Keyword*)kw;
        CGFloat keywordWidth = 0;
        CGFloat keywordHeight = 0;
        size = [self updateLabel:keywordLabels[keywordIndex*keywordParts] withText:[NSString stringWithFormat:@"%@・%@", keyword.word, [keyword.kana substringToIndex:keyword.kanaHighlightStart]]];
        keywordWidth += size.width + 1;
        keywordHeight = MAX(keywordHeight,size.height);
        size = [self updateLabel:keywordLabels[keywordIndex*keywordParts + 1] withText:[keyword.kana substringWithRange:NSMakeRange(keyword.kanaHighlightStart, keyword.kanaHighlightLength)]];
        keywordWidth += size.width + 1;
        keywordHeight = MAX(keywordHeight,size.height);
        NSUInteger tmp = keyword.kanaHighlightStart+keyword.kanaHighlightLength;
        size = [self updateLabel:keywordLabels[keywordIndex*keywordParts + 2] withText:[NSString stringWithFormat:@"%@・%@", [keyword.kana substringWithRange:NSMakeRange(tmp, [keyword.kana length]-tmp)], [keyword.romaji substringToIndex:keyword.romajiHighlightStart]]];
        keywordWidth += size.width + 1;
        keywordHeight = MAX(keywordHeight,size.height);
        size = [self updateLabel:keywordLabels[keywordIndex*keywordParts + 3] withText:[keyword.romaji substringWithRange:NSMakeRange(keyword.romajiHighlightStart, keyword.romajiHighlightLength)]];
        keywordWidth += size.width + 1;
        keywordHeight = MAX(keywordHeight,size.height);
        tmp = keyword.romajiHighlightStart+keyword.romajiHighlightLength;
        size = [self updateLabel:keywordLabels[keywordIndex*keywordParts + 4] withText:[NSString stringWithFormat:@"%@・%@", [keyword.romaji substringWithRange:NSMakeRange(tmp, [keyword.romaji length]-tmp)], keyword.meaning]];
        keywordWidth += size.width + 1;
        keywordHeight = MAX(keywordHeight,size.height);
        keywordSizes[keywordIndex].width = keywordWidth;
        keywordSizes[keywordIndex].height = keywordHeight;
        width = MAX(keywordWidth, size.width);
        height += keywordHeight;
        keywordIndex++;
    }
    NSInteger xOffset, yOffset;
    if (!isPreviewMode) {
        xOffset = 20 + arc4random_uniform([self bounds].size.width - 40 - width);
        yOffset = 20 + arc4random_uniform([self bounds].size.height - 40 - height);
    } else {
        xOffset = ([self bounds].size.width-width)/2;
        yOffset = ([self bounds].size.height-height)/2;;
    }
    NSInteger drawYOffset = yOffset+height;
    size = [kanjiLabel intrinsicContentSize];
    drawYOffset -= size.height;
    [kanjiLabel setFrame:NSMakeRect(xOffset+(width - size.width)/2, drawYOffset, size.width+1, size.height)];
    size = [meaningLabel intrinsicContentSize];
    drawYOffset -= size.height;
    [meaningLabel setFrame:NSMakeRect(xOffset+(width - size.width)/2, drawYOffset, size.width+1, size.height)];
    for (keywordIndex = 0; keywordIndex<[kanji.keywords count]; keywordIndex++) {
        CGFloat partXOffset = xOffset + (width - keywordSizes[keywordIndex].width)/2;
        drawYOffset -= keywordSizes[keywordIndex].height;
        for (int nPart = 0; nPart<keywordParts; nPart++) {
            size = [keywordLabels[keywordIndex*keywordParts+nPart] intrinsicContentSize];
            [keywordLabels[keywordIndex*keywordParts+nPart] setFrame:NSMakeRect(partXOffset, drawYOffset, size.width+1, size.height)];
            partXOffset += size.width + 1;
        }
    }
}

@end
