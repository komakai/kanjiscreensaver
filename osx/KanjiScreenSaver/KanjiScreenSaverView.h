//
//

#import <ScreenSaver/ScreenSaver.h>

@interface KanjiScreenSaverView : ScreenSaverView {
    __weak IBOutlet NSButton *checkBox100;
    __weak IBOutlet NSButton *checkBox90;
    __weak IBOutlet NSButton *checkBox80;
    __weak IBOutlet NSButton *checkBox70;
    __weak IBOutlet NSButton *checkBox60;
    __weak IBOutlet NSButton *checkBox50;
    __weak IBOutlet NSButton *checkBox40;
    __weak IBOutlet NSButton *checkBox30;
    __weak IBOutlet NSButton *checkBox25;
    __weak IBOutlet NSButton *checkBox20;
    __weak IBOutlet NSButton *checkBox15;
    __weak IBOutlet NSButton *checkBox10;
    __weak IBOutlet NSTextField *includeKanjisEdit;
    __weak IBOutlet NSTextField *excludeKanjisEdit;
    __weak IBOutlet NSTextField *fontSample;
    __weak IBOutlet NSPopUpButton *fontCombo;
    __weak IBOutlet NSSlider *durationSlider;
    __weak IBOutlet NSTextField *durationText;
    __weak IBOutlet NSButton *okButton;
    __weak IBOutlet NSButton *cancelButton;

}

// outlets in preferences window
@property (assign) IBOutlet NSWindow *configWindow;

@end
